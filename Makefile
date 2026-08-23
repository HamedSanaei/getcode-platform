.PHONY: infra-up infra-down app-up app-down backend-build backend-test frontend-install frontend-check verify

infra-up:
	docker compose up -d postgres redis

infra-down:
	docker compose down

app-up:
	docker compose --profile app up -d --build

app-down:
	docker compose --profile app down

backend-build:
	dotnet build GetCode.sln --configuration Release

backend-test:
	dotnet test GetCode.sln --configuration Release --no-restore

frontend-install:
	cd frontend && npm install

frontend-check:
	cd frontend && npm run lint && npm run typecheck && npm run build

verify: backend-build backend-test frontend-check
