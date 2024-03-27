.PHONY: publish test validate submit clean

publish:
	dotnet publish -p:PublishSingleFile=true
	mv bin/Release/*/*/publish/ipk24chat-client ipk24chat-client

test:
	cd tests && dotnet test

validate: submit
	-rm -rf submit
	mkdir submit
	cd submit \
		&& unzip ../xstigl00.zip \
		&& make test \
		&& make \
		&& ./ipk24chat-client -h

submit:
	zip xstigl00.zip Makefile LICENSE *.md \
		`find . -name '*.cs' -o -name '*.csproj'`

clean:
	dotnet clean
	cd tests && dotnet clean
	-rm -rf submit
