-- =====================================================================
--  ჩაეწერე (Chaetsere) — MVP database schema
--  PostgreSQL 16+
--
--  Conventions
--    * ids:        uuid (app generates UUIDv7 via Guid.CreateVersion7()),
--                  small lookup tables use integer identity
--    * timestamps: timestamptz, always stored in UTC
--    * work hours: local `time` in the salon's time zone (salons.time_zone)
--    * weekday:    0 = Sunday … 6 = Saturday  (same as .NET DayOfWeek / JS getDay)
--    * money:      numeric(10,2), currency is GEL
--    * enums:      text + CHECK constraint (readable, easy to extend in migrations)
-- =====================================================================

CREATE EXTENSION IF NOT EXISTS btree_gist;   -- needed for the double-booking guard

-- ---------------------------------------------------------------------
-- 1. Reference data
-- ---------------------------------------------------------------------

CREATE TABLE categories (
    id          integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code        varchar(32)  NOT NULL UNIQUE,          -- nails, hair, brows, makeup, spa, epilation
    name        varchar(64)  NOT NULL,                 -- ფრჩხილი, თმა …
    icon        varchar(32)  NOT NULL,
    sort_order  integer      NOT NULL DEFAULT 0
);

CREATE TABLE service_templates (                       -- the onboarding wizard's suggestions
    id                integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    category_id       integer       NOT NULL REFERENCES categories(id),
    name              varchar(120)  NOT NULL,
    duration_minutes  integer       NOT NULL CHECK (duration_minutes BETWEEN 5 AND 720),
    buffer_minutes    integer       NOT NULL DEFAULT 0 CHECK (buffer_minutes BETWEEN 0 AND 120),
    default_price     numeric(10,2) NOT NULL CHECK (default_price >= 0),
    sort_order        integer       NOT NULL DEFAULT 0,
    UNIQUE (category_id, name)
);

CREATE TABLE districts (
    id     integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    city   varchar(64) NOT NULL,                       -- თბილისი, ბათუმი …
    name   varchar(64) NOT NULL,                       -- ვაკე, საბურთალო …
    UNIQUE (city, name)
);

-- ---------------------------------------------------------------------
-- 2. Users & authentication (phone number + SMS code)
-- ---------------------------------------------------------------------

CREATE TABLE users (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    phone              varchar(16)  NOT NULL UNIQUE CHECK (phone ~ '^\+[1-9][0-9]{7,14}$'),  -- E.164: +995599123456
    full_name          varchar(120),
    email              varchar(254),
    is_platform_admin  boolean      NOT NULL DEFAULT false,              -- you: approves salons
    created_at         timestamptz  NOT NULL DEFAULT now(),
    last_login_at      timestamptz,
    deleted_at         timestamptz                                       -- soft delete (account removal request)
);

CREATE TABLE otp_codes (
    id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    phone        varchar(16)  NOT NULL,
    code_hash    varchar(128) NOT NULL,                -- never store the plain code
    expires_at   timestamptz  NOT NULL,
    attempts     smallint     NOT NULL DEFAULT 0 CHECK (attempts >= 0),
    consumed_at  timestamptz,
    request_ip   inet,
    created_at   timestamptz  NOT NULL DEFAULT now()
);
CREATE INDEX ix_otp_codes_phone_created ON otp_codes (phone, created_at DESC);  -- rate limiting + lookup

CREATE TABLE refresh_tokens (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         uuid         NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash      varchar(128) NOT NULL UNIQUE,
    device_name     varchar(120),
    created_at      timestamptz  NOT NULL DEFAULT now(),
    expires_at      timestamptz  NOT NULL,
    revoked_at      timestamptz,
    replaced_by_id  uuid REFERENCES refresh_tokens(id) ON DELETE SET NULL   -- rotation chain
);
CREATE INDEX ix_refresh_tokens_user ON refresh_tokens (user_id);

CREATE TABLE push_tokens (                              -- Expo push tokens, one row per device
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id       uuid         NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token         varchar(255) NOT NULL UNIQUE,
    platform      varchar(8)   NOT NULL CHECK (platform IN ('ios', 'android')),
    created_at    timestamptz  NOT NULL DEFAULT now(),
    last_seen_at  timestamptz  NOT NULL DEFAULT now()
);
CREATE INDEX ix_push_tokens_user ON push_tokens (user_id);

-- ---------------------------------------------------------------------
-- 3. Salons
-- ---------------------------------------------------------------------

CREATE TABLE salons (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    slug                varchar(80)   NOT NULL UNIQUE,                      -- lumina-studio
    name                varchar(120)  NOT NULL,
    phone               varchar(16),
    description         text,
    district_id         integer       REFERENCES districts(id),
    address             varchar(200),
    latitude            double precision CHECK (latitude  BETWEEN -90  AND 90),
    longitude           double precision CHECK (longitude BETWEEN -180 AND 180),
    cover_image_url     varchar(500),
    time_zone           varchar(64)   NOT NULL DEFAULT 'Asia/Tbilisi',
    status              varchar(16)   NOT NULL DEFAULT 'draft'
                        CHECK (status IN ('draft', 'pending_review', 'published', 'suspended')),
    is_listed           boolean       NOT NULL DEFAULT true,               -- owner's "visible in app" toggle
    rating_avg          numeric(2,1)  CHECK (rating_avg BETWEEN 1 AND 5), -- filled when reviews ship
    rating_count        integer       NOT NULL DEFAULT 0,
    created_by_user_id  uuid          NOT NULL REFERENCES users(id),
    created_at          timestamptz   NOT NULL DEFAULT now(),
    updated_at          timestamptz   NOT NULL DEFAULT now(),
    published_at        timestamptz,
    CHECK ((latitude IS NULL) = (longitude IS NULL)),
    -- a salon can only go live with an address and a pin on the map
    CHECK (status NOT IN ('pending_review', 'published')
           OR (address IS NOT NULL AND latitude IS NOT NULL))
);
CREATE INDEX ix_salons_listing ON salons (district_id) WHERE status = 'published' AND is_listed;

CREATE TABLE salon_settings (                            -- 1:1 with salons ("ჯავშნის წესები")
    salon_id                      uuid PRIMARY KEY REFERENCES salons(id) ON DELETE CASCADE,
    auto_confirm                  boolean  NOT NULL DEFAULT true,
    min_lead_minutes              integer  NOT NULL DEFAULT 60   CHECK (min_lead_minutes BETWEEN 0 AND 10080),
    max_advance_days              integer  NOT NULL DEFAULT 30   CHECK (max_advance_days BETWEEN 1 AND 365),
    client_cancel_cutoff_minutes  integer  NOT NULL DEFAULT 120  CHECK (client_cancel_cutoff_minutes >= 0),
    slot_step_minutes             smallint NOT NULL DEFAULT 30   CHECK (slot_step_minutes IN (5, 10, 15, 20, 30, 60)),
    sms_reminders_enabled         boolean  NOT NULL DEFAULT true,
    reminder_minutes_before       integer  NOT NULL DEFAULT 120  CHECK (reminder_minutes_before BETWEEN 15 AND 2880)
);

CREATE TABLE salon_photos (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    salon_id    uuid         NOT NULL REFERENCES salons(id) ON DELETE CASCADE,
    url         varchar(500) NOT NULL,
    sort_order  integer      NOT NULL DEFAULT 0,
    created_at  timestamptz  NOT NULL DEFAULT now()
);
CREATE INDEX ix_salon_photos_salon ON salon_photos (salon_id, sort_order);

CREATE TABLE salon_working_hours (                       -- weekly template; no row = closed that day
    salon_id  uuid     NOT NULL REFERENCES salons(id) ON DELETE CASCADE,
    weekday   smallint NOT NULL CHECK (weekday BETWEEN 0 AND 6),
    opens_at  time     NOT NULL,
    closes_at time     NOT NULL,
    PRIMARY KEY (salon_id, weekday),
    CHECK (opens_at < closes_at)
);

CREATE TABLE salon_closures (                            -- holidays / one-off closed days
    id        uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    salon_id  uuid         NOT NULL REFERENCES salons(id) ON DELETE CASCADE,
    date      date         NOT NULL,
    reason    varchar(200),
    UNIQUE (salon_id, date)
);

-- ---------------------------------------------------------------------
-- 4. Catalog (services)
-- ---------------------------------------------------------------------

CREATE TABLE services (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    salon_id          uuid          NOT NULL REFERENCES salons(id) ON DELETE CASCADE,
    category_id       integer       NOT NULL REFERENCES categories(id),
    name              varchar(120)  NOT NULL,
    description       text,
    duration_minutes  integer       NOT NULL CHECK (duration_minutes BETWEEN 5 AND 720),
    buffer_minutes    integer       NOT NULL DEFAULT 0 CHECK (buffer_minutes BETWEEN 0 AND 120),  -- cleanup after
    price             numeric(10,2) NOT NULL CHECK (price >= 0),
    price_is_from     boolean       NOT NULL DEFAULT false,      -- "-დან" price
    is_online         boolean       NOT NULL DEFAULT true,       -- bookable from the app
    is_archived       boolean       NOT NULL DEFAULT false,      -- never hard-delete: bookings reference it
    sort_order        integer       NOT NULL DEFAULT 0,
    created_at        timestamptz   NOT NULL DEFAULT now(),
    updated_at        timestamptz   NOT NULL DEFAULT now(),
    UNIQUE (id, salon_id)                                        -- target for same-salon composite FKs
);
CREATE INDEX ix_services_salon ON services (salon_id, sort_order) WHERE NOT is_archived;

-- ---------------------------------------------------------------------
-- 5. Staff (owner, manager, masters) and their schedules
-- ---------------------------------------------------------------------

CREATE TABLE staff_members (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    salon_id        uuid         NOT NULL REFERENCES salons(id) ON DELETE CASCADE,
    user_id         uuid         REFERENCES users(id) ON DELETE SET NULL,  -- null until the invite is accepted
    display_name    varchar(120) NOT NULL,
    phone           varchar(16)  NOT NULL CHECK (phone ~ '^\+[1-9][0-9]{7,14}$'),
    role            varchar(16)  NOT NULL CHECK (role IN ('owner', 'manager', 'master')),
    is_bookable     boolean      NOT NULL DEFAULT true,       -- shows up as a calendar column / in the app
    specialization  varchar(120),
    avatar_url      varchar(500),
    color_index     smallint     NOT NULL DEFAULT 0,
    status          varchar(16)  NOT NULL DEFAULT 'invited'
                    CHECK (status IN ('invited', 'active', 'archived')),
    sort_order      integer      NOT NULL DEFAULT 0,
    invited_at      timestamptz  NOT NULL DEFAULT now(),
    joined_at       timestamptz,
    UNIQUE (id, salon_id),
    UNIQUE (salon_id, phone)
);
CREATE UNIQUE INDEX ux_staff_salon_user ON staff_members (salon_id, user_id) WHERE user_id IS NOT NULL;
CREATE INDEX ix_staff_user ON staff_members (user_id) WHERE user_id IS NOT NULL;

CREATE TABLE staff_services (                            -- which master does which service (M:N)
    staff_id    uuid NOT NULL,
    service_id  uuid NOT NULL,
    salon_id    uuid NOT NULL,
    PRIMARY KEY (staff_id, service_id),
    -- both sides must belong to the same salon
    FOREIGN KEY (staff_id,   salon_id) REFERENCES staff_members (id, salon_id) ON DELETE CASCADE,
    FOREIGN KEY (service_id, salon_id) REFERENCES services      (id, salon_id) ON DELETE CASCADE
);
CREATE INDEX ix_staff_services_service ON staff_services (service_id);

CREATE TABLE staff_working_hours (                       -- weekly template per master; no row = day off
    staff_id         uuid     NOT NULL REFERENCES staff_members(id) ON DELETE CASCADE,
    weekday          smallint NOT NULL CHECK (weekday BETWEEN 0 AND 6),
    starts_at        time     NOT NULL,
    ends_at          time     NOT NULL,
    break_starts_at  time,
    break_ends_at    time,
    PRIMARY KEY (staff_id, weekday),
    CHECK (starts_at < ends_at),
    CHECK ((break_starts_at IS NULL) = (break_ends_at IS NULL)),
    CHECK (break_starts_at IS NULL
           OR (break_starts_at >= starts_at AND break_ends_at <= ends_at AND break_starts_at < break_ends_at))
);

CREATE TABLE staff_time_off (                            -- "დროის დაბლოკვა": personal errand, doctor, vacation
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    staff_id            uuid         NOT NULL REFERENCES staff_members(id) ON DELETE CASCADE,
    starts_at           timestamptz  NOT NULL,
    ends_at             timestamptz  NOT NULL,
    reason              varchar(120),
    created_by_user_id  uuid         REFERENCES users(id) ON DELETE SET NULL,
    created_at          timestamptz  NOT NULL DEFAULT now(),
    CHECK (starts_at < ends_at)
);
CREATE INDEX ix_staff_time_off_range ON staff_time_off USING gist (staff_id, tstzrange(starts_at, ends_at));

-- ---------------------------------------------------------------------
-- 6. Clients
-- ---------------------------------------------------------------------

-- A salon's own client card. Exists for app users AND for walk-ins / phone
-- bookings that have no account. The note (allergies etc.) is private to the salon.
CREATE TABLE salon_clients (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    salon_id    uuid         NOT NULL REFERENCES salons(id) ON DELETE CASCADE,
    user_id     uuid         REFERENCES users(id) ON DELETE SET NULL,   -- linked when the phone matches an app user
    full_name   varchar(120) NOT NULL,
    phone       varchar(16)  NOT NULL CHECK (phone ~ '^\+[1-9][0-9]{7,14}$'),
    note        text,
    created_at  timestamptz  NOT NULL DEFAULT now(),
    updated_at  timestamptz  NOT NULL DEFAULT now(),
    UNIQUE (id, salon_id),
    UNIQUE (salon_id, phone)
);
CREATE INDEX ix_salon_clients_user ON salon_clients (user_id) WHERE user_id IS NOT NULL;

CREATE TABLE favorite_salons (
    user_id     uuid        NOT NULL REFERENCES users(id)  ON DELETE CASCADE,
    salon_id    uuid        NOT NULL REFERENCES salons(id) ON DELETE CASCADE,
    created_at  timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, salon_id)
);

-- ---------------------------------------------------------------------
-- 7. Bookings
-- ---------------------------------------------------------------------

CREATE SEQUENCE booking_reference_seq START 1000;      -- human code: CH-1000, CH-1001 …

CREATE TABLE bookings (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    reference_code      varchar(16)   NOT NULL UNIQUE,
    salon_id            uuid          NOT NULL,
    staff_id            uuid          NOT NULL,
    salon_client_id     uuid          NOT NULL,
    created_by_user_id  uuid          REFERENCES users(id) ON DELETE SET NULL,
    source              varchar(8)    NOT NULL CHECK (source IN ('app', 'salon')),   -- აპიდან / ხელით
    status              varchar(16)   NOT NULL
                        CHECK (status IN ('pending', 'confirmed', 'completed', 'no_show', 'cancelled', 'declined')),
    starts_at           timestamptz   NOT NULL,
    ends_at             timestamptz   NOT NULL,           -- end of the last service
    occupied_until      timestamptz   NOT NULL,           -- ends_at + cleanup buffer; used for overlap checks
    total_price         numeric(10,2) NOT NULL CHECK (total_price >= 0),
    client_comment      varchar(500),
    cancel_reason       varchar(200),
    cancelled_by        varchar(8)    CHECK (cancelled_by IN ('client', 'salon')),
    created_at          timestamptz   NOT NULL DEFAULT now(),
    updated_at          timestamptz   NOT NULL DEFAULT now(),
    confirmed_at        timestamptz,
    cancelled_at        timestamptz,
    completed_at        timestamptz,
    reminder_sent_at    timestamptz,

    -- staff and client must belong to the booking's salon
    FOREIGN KEY (staff_id,        salon_id) REFERENCES staff_members (id, salon_id),
    FOREIGN KEY (salon_client_id, salon_id) REFERENCES salon_clients (id, salon_id),
    FOREIGN KEY (salon_id) REFERENCES salons (id),

    CHECK (ends_at > starts_at),
    CHECK (occupied_until >= ends_at),
    CHECK ((status IN ('cancelled', 'declined')) = (cancelled_at IS NOT NULL)),
    CHECK (status <> 'cancelled' OR cancelled_by IS NOT NULL),

    -- THE double-booking guard: one master cannot have two live bookings that overlap.
    -- Two people pressing "დაჯავშნა" at the same second → the second insert fails (SQLSTATE 23P01).
    CONSTRAINT ex_bookings_no_overlap EXCLUDE USING gist (
        staff_id WITH =,
        tstzrange(starts_at, occupied_until) WITH &&
    ) WHERE (status IN ('pending', 'confirmed', 'completed', 'no_show'))
);
CREATE INDEX ix_bookings_salon_start   ON bookings (salon_id, starts_at);          -- salon calendar / lists
CREATE INDEX ix_bookings_client_start  ON bookings (salon_client_id, starts_at DESC); -- client history
CREATE INDEX ix_bookings_creator       ON bookings (created_by_user_id, starts_at DESC)
    WHERE created_by_user_id IS NOT NULL;                                            -- "ჩემი ჯავშნები" in the app
CREATE INDEX ix_bookings_reminder_due  ON bookings (starts_at)
    WHERE status = 'confirmed' AND reminder_sent_at IS NULL;                         -- SMS reminder job

CREATE TABLE booking_services (                          -- snapshot: later price edits don't rewrite history
    booking_id        uuid          NOT NULL REFERENCES bookings(id) ON DELETE CASCADE,
    position          smallint      NOT NULL CHECK (position >= 0),
    service_id        uuid          NOT NULL REFERENCES services(id),   -- services are archived, never deleted
    service_name      varchar(120)  NOT NULL,
    duration_minutes  integer       NOT NULL CHECK (duration_minutes > 0),
    price             numeric(10,2) NOT NULL CHECK (price >= 0),
    PRIMARY KEY (booking_id, position)
);
CREATE INDEX ix_booking_services_service ON booking_services (service_id);

CREATE TABLE booking_events (                            -- audit trail: created, confirmed, moved, cancelled …
    id             bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    booking_id     uuid         NOT NULL REFERENCES bookings(id) ON DELETE CASCADE,
    type           varchar(32)  NOT NULL,
    actor_user_id  uuid         REFERENCES users(id) ON DELETE SET NULL,
    payload        jsonb,                                -- e.g. {"from": "...", "to": "...", "staff_from": "..."}
    created_at     timestamptz  NOT NULL DEFAULT now()
);
CREATE INDEX ix_booking_events_booking ON booking_events (booking_id, created_at);

-- ---------------------------------------------------------------------
-- 8. Notifications
-- ---------------------------------------------------------------------

CREATE TABLE notifications (                             -- in-app feed (bell icon) for clients and staff
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id     uuid         NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    salon_id    uuid         REFERENCES salons(id)   ON DELETE CASCADE,
    booking_id  uuid         REFERENCES bookings(id) ON DELETE SET NULL,
    type        varchar(32)  NOT NULL,                   -- booking_created, booking_cancelled, reminder …
    title       varchar(160) NOT NULL,
    body        varchar(500),
    read_at     timestamptz,
    created_at  timestamptz  NOT NULL DEFAULT now()
);
CREATE INDEX ix_notifications_user ON notifications (user_id, created_at DESC);
CREATE INDEX ix_notifications_unread ON notifications (user_id) WHERE read_at IS NULL;

CREATE TABLE sms_messages (                              -- outbox + log (also tells you what SMS costs)
    id                   uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    phone                varchar(16)  NOT NULL,
    body                 varchar(640) NOT NULL,
    purpose              varchar(16)  NOT NULL
                         CHECK (purpose IN ('otp', 'confirmation', 'reminder', 'rescheduled', 'cancelled', 'invite')),
    booking_id           uuid         REFERENCES bookings(id) ON DELETE SET NULL,
    status               varchar(8)   NOT NULL DEFAULT 'queued' CHECK (status IN ('queued', 'sent', 'failed')),
    provider_message_id  varchar(64),
    error                varchar(500),
    created_at           timestamptz  NOT NULL DEFAULT now(),
    sent_at              timestamptz
);
CREATE INDEX ix_sms_messages_queue ON sms_messages (created_at) WHERE status = 'queued';
CREATE INDEX ix_sms_messages_phone ON sms_messages (phone, created_at DESC);
