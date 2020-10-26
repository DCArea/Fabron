# TGH

This is a background job scheduler built on top of Project Orleans.

It would be:
1) naturly scalable - supported by Orleans' distributed runtime
2) simple enough, the code should be easy to understand
3) performance - high throughput & high efficiency
4) reliable

#### What is it for
It can be use to schedule and execute various sorts of background jobs, for example:
1. Sending a daily newsletter to customers
2. Generate a finance report on every Sunday
3. Give users gift cards on their birthdays
4. Or any other tasks that you need to run it on background

#### Current Status
It's still a side project and I have few ambitions on it, although it already has some small functions like 1) scheduling transient/batch/cron jobs 2) a simple built-in job command & handler to request web APIs.

##### Work Items
* Job Reporter (The job states are managed by Orleans' storage which are not friendly for quering and analysing)
* Monitor/Dashboard (Apparently, it's a necessity for a job scheduler)
* More built-in job commands (Like sending email/sms messages, triggering Azure Functions, etc)
* Make it more reliable (Retry on fails, Callback for job result, etc)
* Make it more generic/usable (Publish core stuff as nuget packages, pack it as docker image or helm chart, make it more configurable, etc)
* CI/CD
* Documentation

Feel free to fork/clone/copy all the code, but do not use it in production directly, it's not tested

#### How to use it
TBD

#### Design Details
TBD

