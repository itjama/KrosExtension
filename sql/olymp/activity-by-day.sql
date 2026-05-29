/*
Run in an OLYMP database.
Parameters:
  @FromDate date inclusive
  @ToDate   date inclusive

Only StartTime is read. UserName, FullName, PcUser, PcName and
AdditionalInfo must not be selected for the dashboard.
*/
SELECT 'olymp.application_activity' AS Metric,
       CONVERT(date, StartTime) AS ActivityDate,
       COUNT_BIG(*) AS ActivityCount
FROM dbo.activityLog
WHERE StartTime >= @FromDate
  AND StartTime < DATEADD(day, 1, @ToDate)
GROUP BY CONVERT(date, StartTime)
ORDER BY ActivityDate;

