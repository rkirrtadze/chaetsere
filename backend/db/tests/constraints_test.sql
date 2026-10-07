-- Rule tests for schema.sql. Run against an EMPTY database after applying schema.sql:
--   psql -v ON_ERROR_STOP=1 -f schema.sql -f tests/constraints_test.sql
-- Every block either succeeds or raises; a failure stops the script.

\set QUIET on
\o /dev/null
BEGIN;

-- helper: expect a statement to fail with a given SQLSTATE
CREATE OR REPLACE FUNCTION pg_temp.expect_error(sql text, state text, label text) RETURNS void AS $$
BEGIN
    EXECUTE sql;
    RAISE EXCEPTION 'FAIL: % — statement succeeded, expected SQLSTATE %', label, state;
EXCEPTION WHEN OTHERS THEN
    IF SQLSTATE = state THEN
        RAISE NOTICE 'ok   %', label;
    ELSE
        RAISE EXCEPTION 'FAIL: % — got SQLSTATE % (%), expected %', label, SQLSTATE, SQLERRM, state;
    END IF;
END $$ LANGUAGE plpgsql;

-- ---------- fixtures ----------
INSERT INTO categories (code, name, icon) VALUES ('nails', 'ფრჩხილი', 'spark'), ('brows', 'წარბი და წამწამი', 'eye');
INSERT INTO districts (city, name) VALUES ('თბილისი', 'ვაკე');

INSERT INTO users (id, phone, full_name) VALUES
  ('00000000-0000-0000-0000-0000000000a1', '+995599123456', 'ლიკა მესხი'),
  ('00000000-0000-0000-0000-0000000000a2', '+995577214365', 'ნინო ბერიძე'),
  ('00000000-0000-0000-0000-0000000000a3', '+995555111222', 'მარიამ ჯაფარიძე');

INSERT INTO salons (id, slug, name, district_id, address, latitude, longitude, status, created_by_user_id)
VALUES ('00000000-0000-0000-0000-00000000005a', 'lumina-studio', 'ლუმინა სტუდიო', 1, 'ჭავჭავაძის გამზ. 12', 41.7095, 44.7613, 'published', '00000000-0000-0000-0000-0000000000a1'),
       ('00000000-0000-0000-0000-00000000005b', 'other-salon',   'სხვა სალონი',   1, 'ვაჟა-ფშაველას 1',  41.72,   44.75,   'published', '00000000-0000-0000-0000-0000000000a1');
INSERT INTO salon_settings (salon_id) VALUES ('00000000-0000-0000-0000-00000000005a'), ('00000000-0000-0000-0000-00000000005b');

INSERT INTO services (id, salon_id, category_id, name, duration_minutes, buffer_minutes, price) VALUES
  ('00000000-0000-0000-0000-0000000005e1', '00000000-0000-0000-0000-00000000005a', 1, 'მანიკური კლასიკური', 40, 5, 30),
  ('00000000-0000-0000-0000-0000000005e2', '00000000-0000-0000-0000-00000000005a', 1, 'გელ-ლაქი', 60, 10, 45),
  ('00000000-0000-0000-0000-0000000005e9', '00000000-0000-0000-0000-00000000005b', 1, 'სხვა სალონის სერვისი', 30, 0, 20);

INSERT INTO staff_members (id, salon_id, user_id, display_name, phone, role, status) VALUES
  ('00000000-0000-0000-0000-00000000057a', '00000000-0000-0000-0000-00000000005a', '00000000-0000-0000-0000-0000000000a2', 'ნინო ბერიძე', '+995577214365', 'master', 'active'),
  ('00000000-0000-0000-0000-00000000057b', '00000000-0000-0000-0000-00000000005a', NULL, 'ანა კაპანაძე', '+995591336721', 'master', 'invited'),
  ('00000000-0000-0000-0000-00000000057c', '00000000-0000-0000-0000-00000000005b', NULL, 'სხვა მასტერი', '+995591000000', 'master', 'active');

INSERT INTO staff_services (staff_id, service_id, salon_id) VALUES
  ('00000000-0000-0000-0000-00000000057a', '00000000-0000-0000-0000-0000000005e1', '00000000-0000-0000-0000-00000000005a'),
  ('00000000-0000-0000-0000-00000000057a', '00000000-0000-0000-0000-0000000005e2', '00000000-0000-0000-0000-00000000005a');

INSERT INTO staff_working_hours (staff_id, weekday, starts_at, ends_at, break_starts_at, break_ends_at)
VALUES ('00000000-0000-0000-0000-00000000057a', 4, '10:00', '19:00', '14:00', '14:30');

INSERT INTO salon_clients (id, salon_id, user_id, full_name, phone) VALUES
  ('00000000-0000-0000-0000-0000000000c1', '00000000-0000-0000-0000-00000000005a', '00000000-0000-0000-0000-0000000000a3', 'მარიამ ჯაფარიძე', '+995555111222'),
  ('00000000-0000-0000-0000-0000000000c2', '00000000-0000-0000-0000-00000000005a', NULL, 'ელენე მაისურაძე', '+995555464767'),
  ('00000000-0000-0000-0000-0000000000c9', '00000000-0000-0000-0000-00000000005b', NULL, 'სხვა კლიენტი', '+995555000000');

-- a live booking: Thu 2026-10-08 15:00–16:40 Tbilisi (+04), buffer until 16:50
INSERT INTO bookings (id, reference_code, salon_id, staff_id, salon_client_id, created_by_user_id, source, status,
                      starts_at, ends_at, occupied_until, total_price, confirmed_at)
VALUES ('00000000-0000-0000-0000-0000000000b1', 'CH-' || nextval('booking_reference_seq'),
        '00000000-0000-0000-0000-00000000005a', '00000000-0000-0000-0000-00000000057a', '00000000-0000-0000-0000-0000000000c1',
        '00000000-0000-0000-0000-0000000000a3', 'app', 'confirmed',
        '2026-10-08 15:00+04', '2026-10-08 16:40+04', '2026-10-08 16:50+04', 75, now());
INSERT INTO booking_services VALUES
  ('00000000-0000-0000-0000-0000000000b1', 0, '00000000-0000-0000-0000-0000000005e1', 'მანიკური კლასიკური', 40, 30),
  ('00000000-0000-0000-0000-0000000000b1', 1, '00000000-0000-0000-0000-0000000005e2', 'გელ-ლაქი', 60, 45);
DO $$ BEGIN RAISE NOTICE 'ok   fixtures inserted (salon, staff, services, clients, booking)'; END $$;

-- ---------- double booking ----------
SELECT pg_temp.expect_error($q$
  INSERT INTO bookings (reference_code, salon_id, staff_id, salon_client_id, source, status, starts_at, ends_at, occupied_until, total_price)
  VALUES ('CH-T1', '00000000-0000-0000-0000-00000000005a', '00000000-0000-0000-0000-00000000057a', '00000000-0000-0000-0000-0000000000c2',
          'salon', 'confirmed', '2026-10-08 16:00+04', '2026-10-08 16:40+04', '2026-10-08 16:40+04', 30)
$q$, '23P01', 'overlapping booking for the same master is rejected');

SELECT pg_temp.expect_error($q$
  INSERT INTO bookings (reference_code, salon_id, staff_id, salon_client_id, source, status, starts_at, ends_at, occupied_until, total_price)
  VALUES ('CH-T2', '00000000-0000-0000-0000-00000000005a', '00000000-0000-0000-0000-00000000057a', '00000000-0000-0000-0000-0000000000c2',
          'salon', 'pending', '2026-10-08 16:45+04', '2026-10-08 17:25+04', '2026-10-08 17:30+04', 30)
$q$, '23P01', 'booking inside the cleanup buffer (16:45) is rejected');

INSERT INTO bookings (reference_code, salon_id, staff_id, salon_client_id, source, status, starts_at, ends_at, occupied_until, total_price)
VALUES ('CH-T3', '00000000-0000-0000-0000-00000000005a', '00000000-0000-0000-0000-00000000057a', '00000000-0000-0000-0000-0000000000c2',
        'salon', 'confirmed', '2026-10-08 16:50+04', '2026-10-08 17:30+04', '2026-10-08 17:35+04', 30);
DO $$ BEGIN RAISE NOTICE 'ok   back-to-back booking right after the buffer (16:50) is accepted'; END $$;

INSERT INTO bookings (reference_code, salon_id, staff_id, salon_client_id, source, status, starts_at, ends_at, occupied_until, total_price)
VALUES ('CH-T4', '00000000-0000-0000-0000-00000000005a', '00000000-0000-0000-0000-00000000057b', '00000000-0000-0000-0000-0000000000c2',
        'salon', 'confirmed', '2026-10-08 15:00+04', '2026-10-08 15:40+04', '2026-10-08 15:45+04', 30);
DO $$ BEGIN RAISE NOTICE 'ok   same time with a different master is accepted'; END $$;

UPDATE bookings SET status = 'cancelled', cancelled_by = 'client', cancelled_at = now()
WHERE id = '00000000-0000-0000-0000-0000000000b1';
INSERT INTO bookings (reference_code, salon_id, staff_id, salon_client_id, source, status, starts_at, ends_at, occupied_until, total_price)
VALUES ('CH-T5', '00000000-0000-0000-0000-00000000005a', '00000000-0000-0000-0000-00000000057a', '00000000-0000-0000-0000-0000000000c2',
        'salon', 'confirmed', '2026-10-08 15:00+04', '2026-10-08 15:40+04', '2026-10-08 15:45+04', 30);
DO $$ BEGIN RAISE NOTICE 'ok   a cancelled booking frees its time slot'; END $$;

-- ---------- cross-salon integrity ----------
SELECT pg_temp.expect_error($q$
  INSERT INTO bookings (reference_code, salon_id, staff_id, salon_client_id, source, status, starts_at, ends_at, occupied_until, total_price)
  VALUES ('CH-T6', '00000000-0000-0000-0000-00000000005a', '00000000-0000-0000-0000-00000000057c', '00000000-0000-0000-0000-0000000000c2',
          'salon', 'confirmed', '2026-10-09 12:00+04', '2026-10-09 12:30+04', '2026-10-09 12:30+04', 20)
$q$, '23503', 'booking with a master from another salon is rejected');

SELECT pg_temp.expect_error($q$
  INSERT INTO bookings (reference_code, salon_id, staff_id, salon_client_id, source, status, starts_at, ends_at, occupied_until, total_price)
  VALUES ('CH-T7', '00000000-0000-0000-0000-00000000005a', '00000000-0000-0000-0000-00000000057a', '00000000-0000-0000-0000-0000000000c9',
          'salon', 'confirmed', '2026-10-09 12:00+04', '2026-10-09 12:30+04', '2026-10-09 12:30+04', 20)
$q$, '23503', 'booking with a client card from another salon is rejected');

SELECT pg_temp.expect_error($q$
  INSERT INTO staff_services VALUES ('00000000-0000-0000-0000-00000000057a', '00000000-0000-0000-0000-0000000005e9', '00000000-0000-0000-0000-00000000005a')
$q$, '23503', 'assigning another salon''s service to a master is rejected');

-- ---------- checks ----------
SELECT pg_temp.expect_error($q$
  UPDATE bookings SET status = 'cancelled' WHERE reference_code = 'CH-T3'
$q$, '23514', 'cancelling without cancelled_at / cancelled_by is rejected');

SELECT pg_temp.expect_error($q$
  INSERT INTO users (phone) VALUES ('599123456')
$q$, '23514', 'phone not in E.164 format is rejected');

SELECT pg_temp.expect_error($q$
  INSERT INTO users (phone) VALUES ('+995599123456')
$q$, '23505', 'duplicate phone for users is rejected');

SELECT pg_temp.expect_error($q$
  INSERT INTO salon_clients (salon_id, full_name, phone) VALUES ('00000000-0000-0000-0000-00000000005a', 'დუბლიკატი', '+995555464767')
$q$, '23505', 'same phone twice in one salon''s client list is rejected');

SELECT pg_temp.expect_error($q$
  INSERT INTO salons (slug, name, status, created_by_user_id) VALUES ('no-address', 'მისამართის გარეშე', 'published', '00000000-0000-0000-0000-0000000000a1')
$q$, '23514', 'publishing a salon without address and map pin is rejected');

SELECT pg_temp.expect_error($q$
  INSERT INTO staff_working_hours VALUES ('00000000-0000-0000-0000-00000000057a', 5, '10:00', '19:00', '18:30', '19:30')
$q$, '23514', 'a break outside working hours is rejected');

SELECT pg_temp.expect_error($q$
  INSERT INTO salon_settings (salon_id) VALUES ('00000000-0000-0000-0000-00000000005a')
$q$, '23505', 'second settings row for the same salon is rejected');

SELECT pg_temp.expect_error($q$
  UPDATE salon_settings SET slot_step_minutes = 25 WHERE salon_id = '00000000-0000-0000-0000-00000000005a'
$q$, '23514', 'an unsupported slot step (25 min) is rejected');

SELECT pg_temp.expect_error($q$
  DELETE FROM services WHERE id = '00000000-0000-0000-0000-0000000005e1'
$q$, '23503', 'deleting a service that bookings reference is blocked (archive it instead)');

DO $$ BEGIN RAISE NOTICE 'ALL CHECKS PASSED'; END $$;
ROLLBACK;
