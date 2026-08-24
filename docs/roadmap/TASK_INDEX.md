# Task index

> Status values: `TODO`, `IN_PROGRESS`, `BLOCKED`, `DONE`.

| Task | Milestone | Priority | Status | Depends on | Title |
|---|---|---:|---|---|---|
| [M00-001](../../docs/tasks/M00-001-lock-supported-toolchain-and-dependency-baselines.md) | M00 | P0 | DONE | None | Lock supported toolchain and dependency baselines |
| [M00-002](../../docs/tasks/M00-002-enforce-clean-dependency-and-module-boundaries.md) | M00 | P0 | DONE | M00-001 | Enforce clean dependency and module boundaries |
| [M00-003](../../docs/tasks/M00-003-establish-local-postgresql-redis-development-environment.md) | M00 | P0 | DONE | M00-001 | Establish local PostgreSQL/Redis development environment |
| [M00-004](../../docs/tasks/M00-004-harden-ci-quality-gates.md) | M00 | P0 | DONE | M00-001, M00-002 | Harden CI quality gates |
| [M00-005](../../docs/tasks/M00-005-build-integration-test-infrastructure.md) | M00 | P0 | DONE | M00-003 | Build integration-test infrastructure |
| [M00-006](../../docs/tasks/M00-006-define-durable-identifiers-and-database-migration-policy.md) | M00 | P0 | DONE | M00-005 | Define durable identifiers and database migration policy |
| [M00-007](../../docs/tasks/M00-007-complete-structured-logging-and-archive-verification.md) | M00 | P0 | DONE | M00-001 | Complete structured logging and archive verification |
| [M00-008](../../docs/tasks/M00-008-add-tracing-metrics-foundation.md) | M00 | P1 | DONE | M00-007 | Add tracing/metrics foundation |
| [M00-009](../../docs/tasks/M00-009-agent-handoff-and-repository-governance-gate.md) | M00 | P1 | DONE | M00-004 | Agent handoff and repository governance gate |
| [M01-001](../../docs/tasks/M01-001-define-ux-sitemap-and-critical-user-flows-in-penpot.md) | M01 | P0 | DONE | M00-009 | Define UX sitemap and critical user flows in Penpot |
| [M01-002](../../docs/tasks/M01-002-create-penpot-foundations-and-token-taxonomy.md) | M01 | P0 | DONE | M01-001 | Create Penpot foundations and token taxonomy |
| [M01-003](../../docs/tasks/M01-003-create-reusable-penpot-component-library.md) | M01 | P0 | DONE | M01-002 | Create reusable Penpot component library |
| [M01-004](../../docs/tasks/M01-004-implement-penpot-to-code-design-token-bridge.md) | M01 | P0 | DONE | M01-002, M00-001 | Implement Penpot-to-code design token bridge |
| [M01-005](../../docs/tasks/M01-005-implement-shared-next-js-ui-primitives.md) | M01 | P0 | TODO | M01-003, M01-004 | Implement shared Next.js UI primitives |
| [M01-006](../../docs/tasks/M01-006-implement-host-aware-application-shell-and-canonical-metadata.md) | M01 | P0 | TODO | M01-004 | Implement host-aware application shell and canonical metadata |
| [M01-007](../../docs/tasks/M01-007-establish-visual-regression-harness.md) | M01 | P1 | TODO | M01-005, M01-006 | Establish visual regression harness |
| [M02-001](../../docs/tasks/M02-001-implement-identity-model-and-authentication-service.md) | M02 | P0 | DONE | M00-006 | Implement identity model and authentication service |
| [M02-002](../../docs/tasks/M02-002-implement-secure-host-scoped-session-token-strategy.md) | M02 | P0 | TODO | M02-001, M01-006 | Implement secure host-scoped session/token strategy |
| [M02-003](../../docs/tasks/M02-003-implement-csrf-cors-and-trusted-redirect-policy.md) | M02 | P0 | TODO | M02-002 | Implement CSRF, CORS and trusted redirect policy |
| [M02-004](../../docs/tasks/M02-004-implement-roles-permissions-and-admin-authorization.md) | M02 | P0 | DONE | M02-001 | Implement roles/permissions and admin authorization |
| [M02-005](../../docs/tasks/M02-005-decide-and-document-cross-domain-sso-v1-scope.md) | M02 | P1 | TODO | M02-002 | Decide and document cross-domain SSO v1 scope |
| [M03-001](../../docs/tasks/M03-001-implement-canonical-country-and-service-catalog.md) | M03 | P0 | DONE | M00-006 | Implement canonical Country and Service catalog |
| [M03-002](../../docs/tasks/M03-002-implement-product-sku-model-for-virtual-number-offerings.md) | M03 | P0 | DONE | M03-001 | Implement Product/SKU model for virtual-number offerings |
| [M03-003](../../docs/tasks/M03-003-implement-provider-registry-and-canonical-mappings.md) | M03 | P0 | DONE | M03-001 | Implement provider registry and canonical mappings |
| [M03-004](../../docs/tasks/M03-004-implement-catalog-query-read-models.md) | M03 | P1 | DONE | M03-002, M03-003 | Implement catalog/query read models |
| [M04-001](../../docs/tasks/M04-001-define-common-provider-behavioral-contract-suite.md) | M04 | P0 | DONE | M03-003, M00-005 | Define common provider behavioral contract suite |
| [M04-002](../../docs/tasks/M04-002-implement-first-real-provider-adapter.md) | M04 | P0 | TODO | M04-001 | Implement first real provider adapter |
| [M04-003](../../docs/tasks/M04-003-implement-provider-health-and-balance-observation.md) | M04 | P1 | TODO | M04-002 | Implement provider health and balance observation |
| [M04-004](../../docs/tasks/M04-004-implement-offer-normalization-and-short-lived-availability-cache.md) | M04 | P0 | TODO | M04-002, M03-004 | Implement offer normalization and short-lived availability cache |
| [M04-005](../../docs/tasks/M04-005-implement-provider-routing-policy-v1.md) | M04 | P0 | TODO | M04-003, M04-004 | Implement provider routing policy v1 |
| [M04-006](../../docs/tasks/M04-006-implement-safe-failover-and-ambiguous-outcome-reconciliation.md) | M04 | P0 | TODO | M04-005 | Implement safe failover and ambiguous-outcome reconciliation |
| [M04-007](../../docs/tasks/M04-007-add-second-provider-to-prove-abstraction.md) | M04 | P1 | TODO | M04-006 | Add second provider to prove abstraction |
| [M05-001](../../docs/tasks/M05-001-implement-pricing-rules-and-margin-model.md) | M05 | P0 | TODO | M03-002, M04-004 | Implement pricing rules and margin model |
| [M05-002](../../docs/tasks/M05-002-implement-immutable-expiring-quote-snapshots.md) | M05 | P0 | TODO | M05-001 | Implement immutable expiring quote snapshots |
| [M05-003](../../docs/tasks/M05-003-implement-wallet-and-immutable-ledger.md) | M05 | P0 | DONE | M00-006 | Implement wallet and immutable ledger |
| [M05-004](../../docs/tasks/M05-004-implement-idempotent-debit-credit-refund-primitives.md) | M05 | P0 | DONE | M05-003 | Implement idempotent debit/credit/refund primitives |
| [M05-005](../../docs/tasks/M05-005-implement-exchange-rate-source-abstraction-if-required.md) | M05 | P2 | TODO | M05-001 | Implement exchange-rate source abstraction if required |
| [M06-001](../../docs/tasks/M06-001-implement-order-aggregate-and-explicit-state-machine.md) | M06 | P0 | TODO | M05-002 | Implement Order aggregate and explicit state machine |
| [M06-002](../../docs/tasks/M06-002-implement-idempotent-checkout-order-creation.md) | M06 | P0 | TODO | M06-001, M05-004 | Implement idempotent checkout/order creation |
| [M06-003](../../docs/tasks/M06-003-define-payment-gateway-port-and-fake-adapter.md) | M06 | P0 | TODO | M06-001 | Define payment gateway port and fake adapter |
| [M06-004](../../docs/tasks/M06-004-implement-first-payment-gateway-and-verified-callback.md) | M06 | P0 | TODO | M06-003 | Implement first payment gateway and verified callback |
| [M06-005](../../docs/tasks/M06-005-implement-transactional-order-paid-outbox-flow.md) | M06 | P0 | TODO | M06-002, M06-004 | Implement transactional order-paid outbox flow |
| [M07-001](../../docs/tasks/M07-001-implement-durable-fulfillment-request-lease-model.md) | M07 | P0 | TODO | M06-005, M04-006 | Implement durable fulfillment request/lease model |
| [M07-002](../../docs/tasks/M07-002-implement-virtual-number-reservation-orchestration.md) | M07 | P0 | TODO | M07-001 | Implement virtual-number reservation orchestration |
| [M07-003](../../docs/tasks/M07-003-implement-activation-polling-and-normalized-message-receipt.md) | M07 | P0 | TODO | M07-002 | Implement activation polling and normalized message receipt |
| [M07-004](../../docs/tasks/M07-004-implement-expiry-cancel-refund-compensation-workflow.md) | M07 | P0 | TODO | M07-003, M05-004 | Implement expiry/cancel/refund compensation workflow |
| [M07-005](../../docs/tasks/M07-005-implement-reconciliation-and-manual-review-cases.md) | M07 | P0 | TODO | M07-004 | Implement reconciliation and Manual Review cases |
| [M08-001](../../docs/tasks/M08-001-implement-public-catalog-pages-from-approved-penpot-designs.md) | M08 | P0 | TODO | M03-004, M01-007 | Implement public catalog pages from approved Penpot designs |
| [M08-002](../../docs/tasks/M08-002-implement-quote-and-checkout-ux.md) | M08 | P0 | TODO | M06-002, M08-001 | Implement quote and checkout UX |
| [M08-003](../../docs/tasks/M08-003-implement-customer-order-dashboard-ux.md) | M08 | P0 | TODO | M06-005, M01-007 | Implement customer order/dashboard UX |
| [M08-004](../../docs/tasks/M08-004-implement-activation-otp-live-experience.md) | M08 | P0 | TODO | M07-003, M08-003 | Implement activation/OTP live experience |
| [M08-005](../../docs/tasks/M08-005-implement-wallet-payment-history-ux.md) | M08 | P1 | TODO | M05-003, M06-004 | Implement wallet/payment history UX |
| [M09-001](../../docs/tasks/M09-001-implement-admin-shell-and-permission-aware-navigation.md) | M09 | P0 | TODO | M02-004, M01-007 | Implement admin shell and permission-aware navigation |
| [M09-002](../../docs/tasks/M09-002-implement-provider-operations-dashboard.md) | M09 | P1 | TODO | M04-003, M09-001 | Implement provider operations dashboard |
| [M09-003](../../docs/tasks/M09-003-implement-catalog-provider-mapping-management.md) | M09 | P0 | TODO | M03-003, M09-001 | Implement catalog/provider mapping management |
| [M09-004](../../docs/tasks/M09-004-implement-pricing-and-promotion-administration.md) | M09 | P1 | TODO | M05-001, M09-001 | Implement pricing and promotion administration |
| [M09-005](../../docs/tasks/M09-005-implement-order-payment-refund-support-tools.md) | M09 | P0 | TODO | M07-005, M09-001 | Implement order/payment/refund support tools |
| [M09-006](../../docs/tasks/M09-006-implement-audit-event-query-and-retention-policy.md) | M09 | P1 | TODO | M09-005 | Implement audit event query and retention policy |
| [M10-001](../../docs/tasks/M10-001-create-critical-path-browser-e2e-suite.md) | M10 | P0 | TODO | M08-004, M09-005 | Create critical-path browser E2E suite |
| [M10-002](../../docs/tasks/M10-002-run-load-concurrency-and-provider-failure-tests.md) | M10 | P0 | TODO | M10-001 | Run load/concurrency and provider-failure tests |
| [M10-003](../../docs/tasks/M10-003-complete-production-security-hardening.md) | M10 | P0 | TODO | M10-001, M02-003 | Complete production security hardening |
| [M10-004](../../docs/tasks/M10-004-implement-backup-restore-and-migration-runbooks.md) | M10 | P0 | TODO | M00-006 | Implement backup, restore and migration runbooks |
| [M10-005](../../docs/tasks/M10-005-validate-logs-metrics-traces-and-operational-alerts.md) | M10 | P0 | TODO | M00-008, M10-002 | Validate logs/metrics/traces and operational alerts |
| [M10-006](../../docs/tasks/M10-006-prepare-first-production-release-gate.md) | M10 | P0 | TODO | M10-003, M10-004, M10-005 | Prepare first production release gate |
| [M11-001](../../docs/tasks/M11-001-evaluate-broker-introduction-from-measured-workload.md) | M11 | P2 | TODO | M10-002 | Evaluate broker introduction from measured workload |
| [M11-002](../../docs/tasks/M11-002-harden-horizontal-worker-coordination.md) | M11 | P1 | TODO | M10-002 | Harden horizontal worker coordination |
| [M11-003](../../docs/tasks/M11-003-add-a-second-fulfillment-product-type.md) | M11 | P2 | TODO | M10-006 | Add a second fulfillment product type |
| [M11-004](../../docs/tasks/M11-004-design-reseller-public-api-if-product-requires-it.md) | M11 | P2 | TODO | M10-006 | Design reseller/public API if product requires it |
| [M11-005](../../docs/tasks/M11-005-implement-cross-domain-sso-only-if-approved.md) | M11 | P2 | TODO | M02-005, M10-003 | Implement cross-domain SSO only if approved |
