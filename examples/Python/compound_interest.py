import json
import sys
from urllib.request import Request, urlopen

base_url = sys.argv[1].rstrip("/") if len(sys.argv) > 1 else "http://localhost:5146"
payload = {"principal": 1000, "ratePerPeriod": 0.01, "periods": 12}
request = Request(
    f"{base_url}/api/v1/finance/compound-interest",
    data=json.dumps(payload).encode("utf-8"),
    headers={"Content-Type": "application/json"},
)
with urlopen(request, timeout=10) as response:
    print(response.read().decode("utf-8"))
