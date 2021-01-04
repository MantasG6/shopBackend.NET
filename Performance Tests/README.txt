Performance tests made using Apache Jmeter
Internal errors on DELETE and POST requests were caused by Apache Jmeters trials to do DELETE or POST concurrently by multiple users
with the same request (same client personal code), this scenario would very less likely to happen in real practice (unless website is attacked and this is done intetionally).
Open results/index.html file to see results
Jmeter performance testing execution command:
jmeter -n -t "{projectDirectory}\shop\Performance Tests\performance_test_plan.jmx" -l "{projectDirectory}\shop\Performance Tests\results\log.jtl" -e -o "{projectDirectory}\shop\Performance Tests\results"