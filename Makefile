.PHONY: publish

publish:
	dotnet publish -p:PublishSingleFile=true
	mv bin/Release/*/*/publish/ipk24chat-client ipk24chat-client
