# ჩაეწერე — Backend

.NET 10 · ASP.NET Core · EF Core 10 · PostgreSQL 16

```
backend/
├── Chaetsere.slnx
├── docker-compose.yml               # ლოკალური PostgreSQL
├── db/
│   ├── schema.sql                   # სრული სქემა (საცნობარო + ტესტებისთვის)
│   └── tests/constraints_test.sql   # ბაზის წესების ტესტები
├── docs/
│   ├── database.md                  # მოდელის აღწერა ← დაიწყე აქედან
│   ├── er-diagram.mmd
│   └── er-diagram.png
└── src/
    ├── Chaetsere.Domain/            # ენტიტები და enum-ები (EF-ის გარეშე)
    └── Chaetsere.Infrastructure/    # AppDbContext, კონფიგურაციები, migration-ის დამატებითი SQL
```

დეტალები: [docs/database.md](docs/database.md)
