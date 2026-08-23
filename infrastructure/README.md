# Infrastructure

The starter uses Caddy as an example edge proxy because one deployment must accept multiple hosts and route `/api/*` to ASP.NET while serving Next.js on the same origin.

Production infrastructure remains replaceable (Cloudflare + Nginx, managed ingress, Kubernetes, etc.). Do not leak edge-specific concepts into Domain/Application.

Logs must be mounted on durable storage. The application writes active daily JSONL logs and archives closed days under `logs/YYYY/MM/<service>/...jsonl.gz`; deleting a `YYYY/MM` folder is a supported manual retention operation.
