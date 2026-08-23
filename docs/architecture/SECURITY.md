# Security baseline

Security is implemented in milestones, but these rules apply immediately.

- Secrets only via secret manager/environment, never repository/appsettings examples.
- Validate forwarded headers at the real deployment edge; production proxy trust must be narrowed during deployment hardening.
- Host allow-list is mandatory in production.
- Credentialed CORS never uses wildcard origin.
- Cookie auth requires Secure/HttpOnly/SameSite and CSRF strategy; cross-root-domain cookie sharing is not attempted.
- Webhook/payment callbacks require provider/gateway authenticity verification plus replay/idempotency defense.
- Admin is permission-based with strong authentication/2FA plan and complete audit events.
- Provider/payment API clients have explicit timeouts and bounded retries; non-idempotent calls are never blindly retried.
- Rate limits/abuse controls are required on authentication, quote/order creation and public expensive endpoints.
- Sensitive activation/message retention is minimized and documented.
- Dependency/security scanning is a CI gate before release.
