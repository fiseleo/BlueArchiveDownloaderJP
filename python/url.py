import requests


def get_jp_version() -> str:
    """Get the current JP version from game api."""

    data = requests.get(
        "https://prod-noticeindex.bluearchiveyostar.com/prod/index.json"
    ).json()
    return data["LatestClientVersion"]


def addressables_catalog_url_root(server_info_data_url: str):
    url = server_info_data_url
    res = requests.get(url).json()
    catalog_root = res["ConnectionGroups"][0]["OverrideConnectionGroups"][-1][
        "AddressablesCatalogUrlRoot"
    ]
    assert isinstance(catalog_root, str)
    return catalog_root
