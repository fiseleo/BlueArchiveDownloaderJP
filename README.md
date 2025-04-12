# BlueArchiveDownloaderJP
# "Blue Archive" is a registered trademark of NAT GAMES Co., Ltd. and Yostar Co., Ltd. This repository is not affiliated with NEXON Korea Corp., NEXON GAMES Co., Ltd., and Yostar Co., Ltd. All game resources are copyrighted to their respective owners.


A small project that downloads all assets of the Japan-Server version of Blue Archive . The script updates the assets and even its own parameters on its own, so all you have to do is execute the BAdownloader.exe after every update to get the latest files.

The first startup will automatically download Chrome, which will take a long time.

To specify a specific version of the apk file, run the program with the -f parameter, for example: `BAdownload.exe -f 1.23.456789`

If you want to download without using a Chrome browser, add -d, for example: `BAdownload.exe -f 1.23.456789 -d` (Note: Currently can't download via direct download due to Cloudflare challenge, we are investigating it)

If you want to re-download Xapk use  `-r` , it will delete xapk and redownload, mostly use when Yostar pushed new APK Update

Thanks starik222 leo0215

[Releases](<https://github.com/fiseleo/BlueArchiveDownloaderJP/tree/main> "Title")
