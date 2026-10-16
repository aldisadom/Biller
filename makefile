.PHONY: run update

run:
	docker compose up -d --build

update:
	git reset --hard origin/master
	git fetch origin
	git checkout -f master
	$(MAKE) run
