# Deployment outline

The starter produces three application containers: `api`, `worker`, `web`, plus PostgreSQL/Redis dependencies and an example Caddy edge.

Before production:

- lock the independent domain and canonical SEO host;
- configure DNS/TLS/CDN/WAF;
- replace local passwords with a secret manager;
- narrow forwarded-proxy trust and host allow-lists;
- provision durable PostgreSQL backups/PITR and test restore;
- provision durable log volume/retention;
- run migrations as a controlled deployment step, not concurrent app startup;
- configure health/readiness checks;
- establish rollback and database compatibility policy.
