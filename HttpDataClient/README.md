Comfortable client for HttpClient. Example of use: https://github.com/operki/HttpDataClient/tree/master/HttpDataClientExample file Program.cs. You can copy Log4NetProvider.cs and Log4NetMetricProvider.cs to your project and use it with this package.

With settings of client you can:

- use custom log and metrics providers
- use retries of downloads, catch exceptions to log
- tune download timeout, preload timeout and retries count
- use some download strategy for files
- check base url for relative urls, cancel requests with unsecure http://
- hide secret params from urls to log
- mimic to chrome browser
- load cookies, add server certificate, credentials and proxy, modify ssl validation
- modify ClientHandler, HttpClient constructor and content (like add headers)
- calculate stats of download data from one site, see LoadStatCalc.cs