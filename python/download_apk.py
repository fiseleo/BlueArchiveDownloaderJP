import cloudscraper  # type: ignore
import shutil
from pathlib import Path
from tqdm import tqdm
from urllib.parse import urlencode
from local_info import local_jp_ver
from url import get_jp_version
from lib.env import ENV
from lib.filepath import FP_JP_APK
import argparse

def download_jp_apk_with_ver(version: str):
    package = FP_JP_APK.stem
    code = version.split(".")[-1]
    q = {"versionCode": code, "nc": "arm64-v8a", "sv": 24}
    url = f"https://d.apkpure.com/b/XAPK/{package}?{urlencode(q)}"
    
    print(f"Constructed URL: {url}")  # 调试日志
    
    scraper = cloudscraper.create_scraper()  # type: ignore
    response = scraper.get(url, stream=True)
    
    print(f"Response Status Code: {response.status_code}")  # 调试日志
    
    if not response.headers.get("content-type", "").startswith("application"):
        raise RuntimeError(f"{package} v{version} is not available on apkpure.")
    
    apk_folder = Path("APK")
    apk_folder.mkdir(exist_ok=True)  # 创建APK文件夹，如果不存在
    apk_path = apk_folder / (FP_JP_APK.name + "." + version + ".xapk")  # 将FP_JP_APK文件名附加到APK文件夹路径
    if not apk_path.exists():
        if ENV.DISABLE_TQDM:
            with open(apk_path, "wb") as f:
                for chunk in response.iter_content(chunk_size=None):
                    if chunk:
                        f.write(chunk)
        else:
            with tqdm.wrapattr(  # type: ignore
                response.raw,
                "read",
                desc=f"Download {package} v{version}",
                total=int(response.headers.get("content-length", 0)),
            ) as r_raw:
                with open(apk_path, "wb") as f:
                    shutil.copyfileobj(r_raw, f)
    print(f"APK file: {apk_path}")
    return apk_path  # 返回下载的apk路径

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("-f", dest="forcedVer", required=False)
    args = parser.parse_args()
    if args.forcedVer is None:
        # 获取新版本号
        new_ver = get_jp_version()
    else:
        new_ver = args.forcedVer
    # 调试日志，显示获取的新版本号
    print(f"New version: {new_ver}")

    # 调用下载函数并传递版本号
    download_jp_apk_with_ver(new_ver)
