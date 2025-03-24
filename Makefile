CS=dotnet

all:
	@echo "Build ruzenbot..."
	@$(CS) build

run:
	@echo "Run ruzenbot..."
	@nohup $(CS) run &

publish: all
	@echo "Publish ruzenbot..."
	@$(CS) publish -o build --self-contained

clean:
	@echo "Clean..."
	@rm -r bin obj build
