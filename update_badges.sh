#!/usr/bin/bash

COOKIE="_sp_id.1809=a03cd3a9-977b-4dc2-8fad-80f55f776414.1753967241.73.1759582071.1759568735.fde5ca38-feb4-41b7-98d9-f6d1fdf1e9ad.fa0a06bd-42d4-4fbc-be97-41e64e3a7d95...0; visitor_id=a16ab1e0-9bc9-4968-b545-fc6e2c60c7a9; ph_phc_Jzsm6DTm6V2705zeU5dcNvQDlonOR68XvX2sh1sEOHO_posthog=%7B%22distinct_id%22%3A%225ce8722357de89af513b1b5de3bd85%22%2C%22%24sesid%22%3A%5B1759206541379%2C%22019998e1-4f06-776c-9762-2125f8d433bb%22%2C1759206526726%5D%2C%22%24epp%22%3Atrue%7D; event_filter=all; _gitlab_session=6bd668dbe254df211b63bf37b12093b8; preferred_language=en; _sp_ses.1809=*"

mkdir badges

cd badges

curl -X GET --location "https://g.np0.pl/hamster-wheel/hlinq/-/badges/release.svg" -H "Cookie: $COOKIE" -o release.svg
curl -X GET --location "https://g.np0.pl/hamster-wheel/hlinq/badges/master/pipeline.svg?ignore_skipped=true" -H "Cookie: $COOKIE" -o pipeline.svg
curl -X GET --location "https://g.np0.pl/hamster-wheel/hlinq/badges/master/coverage.svg" -H "Cookie: $COOKIE" -o coverage.svg


scp -r ./ vps:/opt/wordpress/html/wp-content/uploads/hlinq-badges