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
	rm xstigl00.zip
	zip -du xstigl00.zip Makefile LICENSE *.md \
		`find . -type d \( -name bin -o -name obj -o -name submit \) -prune \
			-o -type f \( -name '*.cs' -o -name '*.csproj' \) -print`

clean:
	dotnet clean
	cd tests && dotnet clean
	-rm -rf submit
