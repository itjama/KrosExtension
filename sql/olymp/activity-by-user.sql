/*
Run in an OLYMP database only for an authorised user-detail endpoint.
Parameters:
  @FromDate date inclusive
  @ToDate   date inclusive

UserName identifies the recorded activity user and is personal data.
Do not select FullName, PcUser, PcName or AdditionalInfo. Do not read
A_Uzivatelia for this metric: it contains password and secret fields.
*/
SELECT 'olymp.application_activity_by_user' AS Metric,
       CONVERT(date, StartTime) AS ActivityDate,
       NULLIF(LTRIM(RTRIM(UserName)), '') AS RecordedUserKey,
       COUNT_BIG(*) AS ActivityCount
FROM dbo.activityLog
WHERE StartTime >= @FromDate
  AND StartTime < DATEADD(day, 1, @ToDate)
  AND NULLIF(LTRIM(RTRIM(UserName)), '') IS NOT NULL
GROUP BY CONVERT(date, StartTime), NULLIF(LTRIM(RTRIM(UserName)), '')
ORDER BY ActivityDate, RecordedUserKey;

