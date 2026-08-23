# Backup / restore contract

M10 implements and tests the production procedure. At minimum:

- PostgreSQL automated backups + point-in-time recovery when supported;
- periodic restore drill into an isolated environment;
- secrets/config stored separately from DB backup;
- Redis is not treated as required disaster-recovery truth;
- logs follow their own retention/backup requirements;
- deployment rollback does not assume schema can always be downgraded.
