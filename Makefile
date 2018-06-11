CONFIGURATION="DebugGtk"
DEST_DIR="Debug"

all:
	if [ ! -d "skia_bin" ]; then \
		mkdir skia_bin; cd skia_bin; \
		curl http://xamjenkinsartifact.blob.core.windows.net/components-skiasharp-ubuntu16-public-artifacts/ArtifactsFor-33/4dc51f7f90eac09cebcfb2c11fb55581c1b3bf65/archive.zip --output archive.zip; \
		unzip archive.zip; \
	fi
	nuget restore
	msbuild /p:Configuration=$(CONFIGURATION)
	cp -t bin/$(DEST_DIR) \
	  ./skia_bin/output/native/linux/x64/libSkiaSharp.so \
		./skia_bin/output/native/linux/x64/libHarfBuzzSharp.so \
		./skia_bin/output/SkiaSharp/nuget/lib/net45/SkiaSharp.dll \
		./skia_bin/output/native/uwp/x64/libHarfBuzzSharp.dll \
		./skia_bin/output/SkiaSharp.Views/nuget/lib/net45/SkiaSharp.Views.Desktop.dll \
		./skia_bin/output/SkiaSharp.Views/nuget/lib/net45/SkiaSharp.Views.Gtk.dll \
		./skia_bin/output/SkiaSharp.HarfBuzz/nuget/lib/netstandard1.3/SkiaSharp.HarfBuzz.dll

run:
	mono bin/$(DEST_DIR)/GtkStartup.exe

clean:
	git clean -fdx
