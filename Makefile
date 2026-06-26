# ============================================================================
#  Kentos Core — developer command center
#  Run `make` (or `make help`) to see everything.
# ============================================================================

# ----- pretty output -------------------------------------------------------
BOLD  := \033[1m
DIM   := \033[2m
RED   := \033[0;31m
GREEN := \033[0;32m
YELL  := \033[0;33m
BLUE  := \033[0;36m
NC    := \033[0m

define banner
printf "\n$(BOLD)$(BLUE)━━━━━ %s ━━━━━$(NC)\n" "$(1)"
endef
define ok
printf "$(GREEN)✓$(NC) $(DIM)%s$(NC)\n" "$(1)"
endef
define step
printf "  $(BLUE)▸$(NC) %s\n" "$(1)"
endef

# ----- config --------------------------------------------------------------
SOLUTION          := Kentos.slnx
HOST_DIR          := src/Kentos.Host
HOST_PROJECT      := $(HOST_DIR)/Kentos.Host.csproj
CLI_PROJECT       := tools/Kentos.AdminCli
AUDIT_PROJECT     := src/Kentos.Infrastructure
KEYCLOAK_HOME     := /home/cihad/Projects/keycloak-26.6.3
PG_CONTAINER      := kentos-postgres
HOST_URL          := http://localhost:5080

# Module-scoped defaults (override on the command line for other modules):
MODULE_PROJECT    ?= src/Modules/Kentos.Modules.Settlement
CONTEXT           ?= SettlementDbContext
MODULE_MIGRATIONS ?= Infrastructure/Migrations

.DEFAULT_GOAL := help
SHELL := /bin/bash

# ===========================================================================
##@ Help
# ===========================================================================
.PHONY: help
help: ## Show this help
	@printf "\n$(BOLD)Kentos Core$(NC) $(DIM)— modular monolith command center$(NC)\n"
	@printf "$(DIM)Usage:$(NC) make $(BLUE)<target>$(NC)\n"
	@awk 'BEGIN {FS = ":.*##"} \
		/^##@/ { printf "\n$(BOLD)%s$(NC)\n", substr($$0, 5); next } \
		/^[a-zA-Z0-9_.-]+:.*##/ { printf "  $(BLUE)%-22s$(NC) %s\n", $$1, $$2 }' $(MAKEFILE_LIST)
	@printf "\n$(DIM)Tip:$(NC) $(BOLD)make up$(NC) brings the whole stack online.\n\n"

# ===========================================================================
##@ Lifecycle (one-shot)
# ===========================================================================
.PHONY: up
up: infra-up keycloak-start provision-dev migrate ## Bring the entire local stack online
	@$(call banner,Stack is online)
	@$(call ok,API:        make run  →  $(HOST_URL)/scalar)
	@$(call ok,Keycloak:   http://localhost:8080  (admin/admin))
	@$(call ok,Prometheus: http://localhost:9090   Grafana: http://localhost:3001)
	@printf "\n"

.PHONY: down
down: stop keycloak-stop infra-down ## Stop the app, Keycloak and all infra
	@$(call ok,Everything stopped)

# ===========================================================================
##@ Infrastructure (Docker — keep these running)
# ===========================================================================
.PHONY: infra-up
infra-up: ## Start postgres, mongo, prometheus, grafana, jaeger
	@$(call banner,Starting infrastructure)
	@docker compose up -d postgres mongo prometheus grafana jaeger
	@$(call ok,Infrastructure up)

.PHONY: infra-down
infra-down: ## Stop infrastructure containers
	@docker compose down && $(call ok,Infrastructure down)

.PHONY: infra-restart
infra-restart: infra-down infra-up ## Restart infrastructure

.PHONY: infra-status
infra-status: ## Show infrastructure container status
	@$(call banner,Infrastructure status)
	@docker compose ps

.PHONY: infra-logs
infra-logs: ## Tail infrastructure logs
	@docker compose logs -f --tail=80

# ===========================================================================
##@ Keycloak
# ===========================================================================
.PHONY: keycloak-start
keycloak-start: ## Start local Keycloak (background) and wait until ready
	@$(call banner,Starting Keycloak)
	@if curl -sf -o /dev/null http://localhost:8080/realms/master; then \
		$(call ok,Keycloak already running); \
	else \
		KC_BOOTSTRAP_ADMIN_USERNAME=admin KC_BOOTSTRAP_ADMIN_PASSWORD=admin \
			nohup $(KEYCLOAK_HOME)/bin/kc.sh start-dev --http-port=8080 > /tmp/kentos-keycloak.log 2>&1 & \
		printf "  $(BLUE)▸$(NC) waiting for Keycloak"; \
		for i in $$(seq 1 40); do \
			if curl -sf -o /dev/null http://localhost:8080/realms/master; then break; fi; \
			printf "."; sleep 2; \
		done; printf "\n"; \
		$(call ok,Keycloak ready at http://localhost:8080); \
	fi

.PHONY: keycloak-stop
keycloak-stop: ## Stop local Keycloak
	@pkill -f "kc.sh start-dev" 2>/dev/null && $(call ok,Keycloak stopped) || printf "$(DIM)Keycloak was not running$(NC)\n"

.PHONY: keycloak-logs
keycloak-logs: ## Tail local Keycloak logs
	@tail -f /tmp/kentos-keycloak.log

.PHONY: provision
provision: ## Provision Keycloak (realm, client, mappers, roles) — idempotent
	@$(call banner,Provisioning Keycloak)
	@dotnet run --project $(CLI_PROJECT) -- provision

.PHONY: provision-dev
provision-dev: ## Provision Keycloak + a dev test user
	@$(call banner,Provisioning Keycloak (dev))
	@dotnet run --project $(CLI_PROJECT) -- provision --dev

# ===========================================================================
##@ Application
# ===========================================================================
.PHONY: restore
restore: ## Restore NuGet packages
	@dotnet restore $(SOLUTION) >/dev/null && $(call ok,Packages restored)

.PHONY: build
build: ## Build the whole solution
	@$(call banner,Building solution)
	@dotnet build $(SOLUTION) --nologo -v q && $(call ok,Build succeeded)

.PHONY: run
run: ## Run the API in the foreground (Ctrl-C to stop)
	@$(call banner,Running API → $(HOST_URL)/scalar)
	@cd $(HOST_DIR) && ASPNETCORE_URLS=$(HOST_URL) dotnet run

.PHONY: stop
stop: ## Stop a backgrounded API
	@pkill -f "Kentos.Host.dll" 2>/dev/null && $(call ok,API stopped) || printf "$(DIM)API was not running$(NC)\n"

.PHONY: watch
watch: ## Run the API with hot reload
	@cd $(HOST_DIR) && ASPNETCORE_URLS=$(HOST_URL) dotnet watch run

.PHONY: clean
clean: ## Remove build artifacts
	@dotnet clean $(SOLUTION) --nologo -v q >/dev/null 2>&1; find . -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} + ; $(call ok,Cleaned)

# ===========================================================================
##@ Database & migrations
# ===========================================================================
.PHONY: migrate
migrate: ## Apply all migrations (auditing + settlement)
	@$(call banner,Applying migrations)
	@$(call step,auditing schema)
	@dotnet ef database update --project $(AUDIT_PROJECT) --startup-project $(HOST_PROJECT) --context AuditingDbContext
	@$(call step,settlement schema)
	@dotnet ef database update --project $(MODULE_PROJECT) --startup-project $(HOST_PROJECT) --context $(CONTEXT)
	@$(call ok,Database up to date)

.PHONY: migrate-add
migrate-add: ## Add a migration: make migrate-add NAME=Xxx [MODULE_PROJECT=.. CONTEXT=..]
	@test -n "$(NAME)" || { printf "$(RED)NAME is required: make migrate-add NAME=AddFoo$(NC)\n"; exit 1; }
	@$(call banner,Adding migration $(NAME))
	@dotnet ef migrations add $(NAME) --project $(MODULE_PROJECT) --startup-project $(HOST_PROJECT) --context $(CONTEXT) -o $(MODULE_MIGRATIONS)
	@$(call ok,Migration $(NAME) created)

.PHONY: db-shell
db-shell: ## Open a psql shell on the dev database
	@docker exec -it $(PG_CONTAINER) psql -U kentos -d kentos

.PHONY: db-reset
db-reset: ## Drop and recreate the dev database (DESTRUCTIVE)
	@$(call banner,Resetting database)
	@docker exec $(PG_CONTAINER) psql -U kentos -d postgres -c "DROP DATABASE IF EXISTS kentos;" -c "CREATE DATABASE kentos OWNER kentos;"
	@$(MAKE) --no-print-directory migrate

# ===========================================================================
##@ Permissions
# ===========================================================================
.PHONY: permissions-scan
permissions-scan: ## Regenerate permissions.json from module code
	@$(call banner,Scanning permissions)
	@dotnet run --project $(CLI_PROJECT) -- permissions scan -o permissions.json

.PHONY: permissions-sync
permissions-sync: ## Sync permissions.json to Keycloak client roles
	@$(call banner,Syncing permissions to Keycloak)
	@dotnet run --project $(CLI_PROJECT) -- permissions sync

# ===========================================================================
##@ Tests
# ===========================================================================
.PHONY: test
test: ## Run all tests (needs Docker for Testcontainers)
	@$(call banner,Running all tests)
	@dotnet test $(SOLUTION) --nologo

.PHONY: test-unit
test-unit: ## Run unit tests only (no Docker required)
	@dotnet test tests/Kentos.Modules.Settlement.UnitTests --nologo

.PHONY: test-integration
test-integration: ## Run integration + API tests (Testcontainers)
	@dotnet test tests/Kentos.Modules.Settlement.IntegrationTests --nologo
	@dotnet test tests/Kentos.Api.IntegrationTests --nologo

# ===========================================================================
##@ Quality
# ===========================================================================
.PHONY: format
format: ## Format the codebase
	@dotnet format $(SOLUTION) && $(call ok,Formatted)

.PHONY: outdated
outdated: ## List outdated NuGet packages
	@dotnet list $(SOLUTION) package --outdated
