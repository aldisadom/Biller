.PHONY: run update backup restore

run:
	docker compose up -d --build

update: backup
	git reset --hard origin/master
	git fetch origin
	git checkout -f master
	$(MAKE) run

backup:
	bash scripts/db-backup.sh

restore:
	bash scripts/db-restore.sh
