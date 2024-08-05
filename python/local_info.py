import os
import base64
import json
import re
import UnityPy
from typing import Dict
from lib.TableEncryptionService import ConvertString, CreateKey
from lib.file_info import get_file_size
from lib.filepath import FP_JP_APK
from pathlib import Path




def local_jp_ver():
    """Get the local (extracted) apk version."""
    current_directory = os.path.dirname(__file__)
    apk_assets_path = os.path.join(current_directory,"APK" ,"unzip", "assets", "bin", "Data" )
    path = next(Path(apk_assets_path).with_suffix("").rglob("globalgamemanagers"))
    env = UnityPy.load(path.as_posix())
    obj = next(obj for obj in env.objects if obj.type.name == "PlayerSettings")
    m = re.search(rb"\d+?\.\d+?\.\d+", obj.get_raw_data())
    assert m
    build_version = m[0].decode()
    version_path = os.path.join(current_directory, "..", "version")
    with open(os.path.join(version_path, "apk_version.txt"), "w") as f:
        f.write(build_version)
    print("version extracted successfully!")
    return build_version


def game_main_config():
    ver = local_jp_ver()    
    current_directory = os.path.dirname(__file__)
    apk_assets_path = os.path.join(current_directory, "APK", "unzip", "assets")
    bin_files = Path(apk_assets_path).with_suffix("").rglob("bin/Data/*")
    
    for path in bin_files:
        if get_file_size(path) > 6000:
            continue
        try:
            env = UnityPy.load(path.as_posix())
            for obj in env.objects:
                if obj.type.name != "TextAsset":
                    break
                data = obj.read()
                if data.name != "GameMainConfig":
                    break
                return bytes(data.script)
        except:
            pass
    
    raise RuntimeError("Unable to find GameMainConfig")


def server_info_data_url():
    dat = game_main_config()
    b64 = base64.b64encode(dat).decode("utf-8")
    json_str = ConvertString(b64, CreateKey("GameMainConfig"))
    obj: Dict[str, str] = json.loads(json_str)
    v = obj["X04YXBFqd3ZpTg9cKmpvdmpOElwnamB2eE4cXDZqc3ZgTg=="]
    url = ConvertString(v, CreateKey("ServerInfoDataUrl"))
    current_directory = os.path.dirname(__file__)
    apk_folder = os.path.join(current_directory, "APK")
    with open(os.path.join(apk_folder, "url.txt"), "w") as f:
        f.write(url)
    print("url extracted successfully!")
    return url

if __name__ == "__main__":
    server_info_data_url()
