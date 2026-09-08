# Shortcuts for the commands in README.md / OVERVIEW.md. Everything here is a thin wrapper —
# nothing in the project requires make, it just saves typing. On Windows use `make` from Git
# Bash / WSL, or copy the command from the recipe.
.DEFAULT_GOAL := help
.PHONY: help up demo down reset logs build test test-backend test-frontend test-e2e lint env media

help: ## Show this help
	@grep -hE '^[a-z-]+:.*?## ' $(MAKEFILE_LIST) | awk -F':.*?## ' '{printf "  \033[36m%-14s\033[0m %s\n", $$1, $$2}'

env: ## Create a .env from .env.example (does nothing if .env already exists)
	@test -f .env || (cp .env.example .env && echo "Created .env — edit it to taste.")

up: ## Build and start the full stack
	docker compose up --build -d
	@echo "Web http://localhost:$$(docker compose port frontend 8080 | cut -d: -f2) · API http://localhost:5080/swagger"

demo: ## Start the stack from CI-published images — no local build
	docker compose -f docker-compose.yml -f docker-compose.prebuilt.yml up -d
	@echo "Web http://localhost:$$(docker compose port frontend 8080 | cut -d: -f2) · sign in as demo@taskflow.dev"

down: ## Stop the stack (keeps the database volume)
	docker compose down

reset: ## Stop the stack and wipe all data (the next start re-seeds the demo workspace)
	docker compose down -v

logs: ## Tail the API logs
	docker compose logs api -f

build: ## Build backend and frontend locally (no Docker)
	dotnet build
	cd frontend && npm ci && npm run build

test: test-backend test-frontend ## Run backend + frontend test suites

test-backend: ## xUnit unit + integration tests (integration needs Docker running)
	dotnet test

test-frontend: ## Karma/Jasmine tests, headless
	cd frontend && npx ng test --watch=false --browsers=ChromeHeadless

test-e2e: ## Playwright tests against the real Docker stack
	docker compose -f docker-compose.yml -f docker-compose.e2e.yml up --build -d
	export E2E_BASE_URL=http://localhost:$$(docker compose port frontend 8080 | cut -d: -f2); \
		cd e2e && npm ci && npx playwright install --with-deps chromium && npx playwright test

media: ## Regenerate the README screenshots and demo GIF (needs the seeded stack running)
	export E2E_BASE_URL=http://localhost:$$(docker compose port frontend 8080 | cut -d: -f2); \
		cd e2e && node capture-screenshots.mjs && node capture-demo-frames.mjs && python build-demo-gif.py

lint: ## Angular lint
	cd frontend && npx ng lint
