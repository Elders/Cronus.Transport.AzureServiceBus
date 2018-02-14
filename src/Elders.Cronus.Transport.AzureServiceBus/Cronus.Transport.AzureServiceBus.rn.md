#### 2.0.3 - 14.02.2018
* Adds checks and retries when we manage Azure resource. In general Azure is very restrictive in terms of POST requests. We were rejected at 5-6 concurrent requests.

#### 2.0.2 - 14.02.2018
* Use MD5 for Topic and Subscription names because of reasons => https://feedback.azure.com/forums/216926-service-bus/suggestions/18552391-increase-the-maximum-length-of-the-name-of-a-topic

#### 2.0.1 - 12.02.2018
* Fixes configuration method

#### 2.0.0 - 12.02.2018
* Rewrite the Azure service bus with the newest client from Microsoft => https://github.com/Azure/azure-service-bus-dotnet

#### 1.1.1 - 19.01.2018
* Fixes memory leak on message processing

#### 1.1.0 - 09.01.2018
* Updates to .net framework 4.6.1
* Updates to latest packages
* Updates pipeline/topic naming convention to use bounded context namespace naming convention
* Fixes issue where client is  throwing exception because it is disposed and never recreated

#### 1.0.1 - 20.10.2017
* Initial release
