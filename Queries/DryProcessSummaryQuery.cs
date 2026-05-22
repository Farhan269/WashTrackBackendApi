namespace wsahRecieveDelivary.Queries
{
    public static class DryProcessSummaryQuery
    {
        public const string GetSummary = @"
WITH BaseQc AS
(
    SELECT
        fdpq.Id,
        fdpq.FirstDryProcessId,
        fdpq.WashProcessId,
        fdpq.QcStatusId,
        fdpq.CreateDate,

        -- Operational Date
        CASE
            WHEN CONVERT(TIME, fdpq.CreateDate) >= '08:00:00'
                THEN CONVERT(DATE, fdpq.CreateDate)
            ELSE CONVERT(DATE, DATEADD(DAY,-1,fdpq.CreateDate))
        END AS OperationalDate,

        -- Shift
        CASE
            WHEN CONVERT(TIME, fdpq.CreateDate) >= '08:00:00'
             AND CONVERT(TIME, fdpq.CreateDate) < '20:00:00'
            THEN 1
            ELSE 2
        END AS Shift

    FROM FirstDryProcessQc fdpq

    WHERE
        fdpq.IsDeleted = 0
        AND fdpq.IsActive = 1

        AND
        (
            @FromDate IS NULL
            OR
            (
                CASE
                    WHEN CONVERT(TIME, fdpq.CreateDate) >= '08:00:00'
                        THEN CONVERT(DATE, fdpq.CreateDate)
                    ELSE CONVERT(DATE, DATEADD(DAY,-1,fdpq.CreateDate))
                END
            ) >= @FromDate
        )

        AND
        (
            @ToDate IS NULL
            OR
            (
                CASE
                    WHEN CONVERT(TIME, fdpq.CreateDate) >= '08:00:00'
                        THEN CONVERT(DATE, fdpq.CreateDate)
                    ELSE CONVERT(DATE, DATEADD(DAY,-1,fdpq.CreateDate))
                END
            ) <= @ToDate
        )
),

QcData AS
(
    SELECT
        b.WashProcessId,
        wp.ProcessName,

        fdp.ProcessModuleId,
        pm.Name AS ProcessModuleName,

        pu.PlantId,
        fdp.UnitId,

        b.OperationalDate,
        b.Shift,

        SUM(CASE WHEN b.QcStatusId IN (1,3) THEN 1 ELSE 0 END) AS PassQty,
        SUM(CASE WHEN b.QcStatusId = 2 THEN 1 ELSE 0 END) AS DefectQty,
        SUM(CASE WHEN b.QcStatusId = 4 THEN 1 ELSE 0 END) AS RejectQty

    FROM BaseQc b

    JOIN FirstDryProcess fdp
        ON b.FirstDryProcessId = fdp.Id

    JOIN WashProcess wp
        ON b.WashProcessId = wp.Id

    JOIN ProcessModule pm
        ON fdp.ProcessModuleId = pm.Id

    JOIN PlantUnit pu
        ON fdp.UnitId = pu.Id

    WHERE
        (@PlantIds IS NULL
            OR pu.PlantId IN (
                SELECT TRY_CAST(value AS INT)
                FROM STRING_SPLIT(@PlantIds, ',')
            )
        )

        AND (@UnitIds IS NULL
            OR fdp.UnitId IN (
                SELECT TRY_CAST(value AS INT)
                FROM STRING_SPLIT(@UnitIds, ',')
            )
        )

        AND (@ProcessModuleIds IS NULL
            OR fdp.ProcessModuleId IN (
                SELECT TRY_CAST(value AS INT)
                FROM STRING_SPLIT(@ProcessModuleIds, ',')
            )
        )

        AND (@WashProcessIds IS NULL
            OR b.WashProcessId IN (
                SELECT TRY_CAST(value AS INT)
                FROM STRING_SPLIT(@WashProcessIds, ',')
            )
        )

        AND (@ShiftList IS NULL
            OR b.Shift IN (
                SELECT TRY_CAST(value AS INT)
                FROM STRING_SPLIT(@ShiftList, ',')
            )
        )

    GROUP BY
        b.WashProcessId,
        wp.ProcessName,
        fdp.ProcessModuleId,
        pm.Name,
        pu.PlantId,
        fdp.UnitId,
        b.OperationalDate,
        b.Shift
),

IssueData AS
(
    SELECT
        fdp.UnitId,
        fdp.ProcessModuleId,
        b.WashProcessId,

        b.OperationalDate,
        b.Shift,

        COUNT_BIG(*) AS IssueQty

    FROM FirstDryProcessQcIsuee qi

    JOIN BaseQc b
        ON qi.FirstDryProcessQcId = b.Id

    JOIN FirstDryProcess fdp
        ON b.FirstDryProcessId = fdp.Id

    GROUP BY
        fdp.UnitId,
        fdp.ProcessModuleId,
        b.WashProcessId,
        b.OperationalDate,
        b.Shift
),

TargetData AS
(
    SELECT
        wh.WorkingHourDay AS OperationalDate,
        wh.UnitId,
        wp.ProcessModuleId,
        whdp.WashProcessId,

        CASE
            WHEN whd.StartTime >= '08:00:00'
             AND whd.EndTime < '20:00:00'
            THEN 1
            ELSE 2
        END AS Shift,

        SUM(whdp.DailyTarget) AS DayTarget,
        AVG(CAST(whdp.ManPower AS DECIMAL(18,2))) AS ManPower,
        AVG(CAST(whdp.SMV AS DECIMAL(18,2))) AS SMV

    FROM WorkingHourDetailManPower whdp

    JOIN WorkingHourDetail whd
        ON whdp.WorkingHourDetailId = whd.Id

    JOIN WorkingHour wh
        ON whd.WorkingHourId = wh.Id

    JOIN WashProcess wp
        ON whdp.WashProcessId = wp.Id

    WHERE
        whdp.IsActive = 1
        AND whdp.IsDeleted = 0

        AND (@FromDate IS NULL OR wh.WorkingHourDay >= @FromDate)
        AND (@ToDate IS NULL OR wh.WorkingHourDay <= @ToDate)

    GROUP BY
        wh.WorkingHourDay,
        wh.UnitId,
        wp.ProcessModuleId,
        whdp.WashProcessId,

        CASE
            WHEN whd.StartTime >= '08:00:00'
             AND whd.EndTime < '20:00:00'
            THEN 1
            ELSE 2
        END
)

SELECT
    q.ProcessModuleId,
    q.ProcessModuleName,

    q.WashProcessId,
    q.ProcessName,

    SUM(q.PassQty) AS PassQty,
    SUM(q.DefectQty) AS DefectQty,
    SUM(q.RejectQty) AS RejectQty,

    SUM(ISNULL(i.IssueQty,0)) AS IssueQty,

    SUM(ISNULL(t.DayTarget,0)) AS DayTarget,
    AVG(ISNULL(t.ManPower,0)) AS ManPower,
    AVG(ISNULL(t.SMV,0)) AS SMV,

    -- DHU
    CASE
        WHEN SUM(q.PassQty) = 0 THEN 0
        ELSE CAST(
            (SUM(ISNULL(i.IssueQty,0)) * 100.0)
            / SUM(q.PassQty)
        AS DECIMAL(18,2))
    END AS DHU,

    -- PlanEff
    CASE
        WHEN AVG(ISNULL(t.ManPower,0)) = 0
          OR AVG(ISNULL(t.SMV,0)) = 0
        THEN 0
        ELSE CAST(
            (SUM(ISNULL(t.DayTarget,0))
            * AVG(ISNULL(t.SMV,0))
            * 100.0)
            /
            (11 * AVG(ISNULL(t.ManPower,0)) * 60)
        AS DECIMAL(18,2))
    END AS PlanEff,

    -- ActualEff
    CASE
        WHEN AVG(ISNULL(t.ManPower,0)) = 0
          OR AVG(ISNULL(t.SMV,0)) = 0
        THEN 0
        ELSE CAST(
            (SUM(q.PassQty)
            * AVG(ISNULL(t.SMV,0))
            * 100.0)
            /
            (11 * AVG(ISNULL(t.ManPower,0)) * 60)
        AS DECIMAL(18,2))
    END AS ActualEff

FROM QcData q

LEFT JOIN IssueData i
    ON q.UnitId = i.UnitId
    AND q.ProcessModuleId = i.ProcessModuleId
    AND q.WashProcessId = i.WashProcessId
    AND q.OperationalDate = i.OperationalDate
    AND q.Shift = i.Shift

LEFT JOIN TargetData t
    ON q.UnitId = t.UnitId
    AND q.ProcessModuleId = t.ProcessModuleId
    AND q.WashProcessId = t.WashProcessId
    AND q.OperationalDate = t.OperationalDate
    AND q.Shift = t.Shift

GROUP BY
    q.ProcessModuleId,
    q.ProcessModuleName,
    q.WashProcessId,
    q.ProcessName

ORDER BY
    q.ProcessModuleName,
    q.ProcessName;
";


        public const string GetTopIssues = @"
WITH Base AS
(
    SELECT
        pm.Id AS ProcessModuleId,
        pm.Name AS ProcessModuleName,

        wp.Id AS WashProcessId,
        wp.ProcessName,

        wpi.Id AS WashProcessIssueId,
        wpi.IssueName,

        pu.PlantId,
        wp.UnitId,

        CASE
            WHEN CAST(fdq.CreateDate AS TIME) >= '08:00:00'
                THEN CAST(fdq.CreateDate AS DATE)
            ELSE CAST(DATEADD(DAY, -1, fdq.CreateDate) AS DATE)
        END AS OperationalDate,

        CASE
            WHEN CAST(fdq.CreateDate AS TIME) >= '08:00:00'
             AND CAST(fdq.CreateDate AS TIME) < '20:00:00'
            THEN 1 ELSE 2
        END AS Shift,

        1 AS IssueQty

    FROM FirstDryProcessQcIsuee fdpqi

    JOIN FirstDryProcessQc fdq
        ON fdpqi.FirstDryProcessQcId = fdq.Id

    JOIN FirstDryProcess fdp
        ON fdq.FirstDryProcessId = fdp.Id

    JOIN ProcessModule pm
        ON fdp.ProcessModuleId = pm.Id

    JOIN WashProcess wp
        ON fdq.WashProcessId = wp.Id   -- ✅ FIX HERE (IMPORTANT)

    JOIN WashProcessIssue wpi
        ON fdpqi.WashProcessIssueId = wpi.Id

    JOIN PlantUnit pu
        ON fdp.UnitId = pu.Id

    WHERE
        fdpqi.IsDeleted = 0
        AND fdpqi.IsActive = 1
        AND fdq.IsDeleted = 0
        AND fdq.IsActive = 1

        AND (
            @FromDate IS NULL OR @ToDate IS NULL
            OR (
                CASE
                    WHEN CAST(fdq.CreateDate AS TIME) >= '08:00:00'
                        THEN CAST(fdq.CreateDate AS DATE)
                    ELSE CAST(DATEADD(DAY, -1, fdq.CreateDate) AS DATE)
                END
            ) BETWEEN @FromDate AND @ToDate
        )

        AND (
            @PlantIds IS NULL
            OR pu.PlantId IN (SELECT TRY_CAST(value AS INT) FROM STRING_SPLIT(@PlantIds, ','))
        )

        AND (
            @UnitIds IS NULL
            OR fdp.UnitId IN (SELECT TRY_CAST(value AS INT) FROM STRING_SPLIT(@UnitIds, ','))
        )

        AND (
            @ProcessModuleIds IS NULL
            OR pm.Id IN (SELECT TRY_CAST(value AS INT) FROM STRING_SPLIT(@ProcessModuleIds, ','))
        )

        AND (
            @WashProcessIds IS NULL
            OR wp.Id IN (SELECT TRY_CAST(value AS INT) FROM STRING_SPLIT(@WashProcessIds, ','))
        )

        AND (
            @ShiftList IS NULL
            OR (
                CASE
                    WHEN CAST(fdq.CreateDate AS TIME) >= '08:00:00'
                     AND CAST(fdq.CreateDate AS TIME) < '20:00:00'
                    THEN 1 ELSE 2
                END
            ) IN (SELECT TRY_CAST(value AS INT) FROM STRING_SPLIT(@ShiftList, ','))
        )
),

Agg AS
(
    SELECT
        ProcessModuleId,
        ProcessModuleName,
        WashProcessId,
        ProcessName,
        WashProcessIssueId,
        IssueName,
        SUM(CAST(IssueQty AS BIGINT)) AS IssueQty
    FROM Base
    GROUP BY
        ProcessModuleId,
        ProcessModuleName,
        WashProcessId,
        ProcessName,
        WashProcessIssueId,
        IssueName
)

SELECT TOP 5
    ProcessModuleId,
    ProcessModuleName,
    WashProcessId,
    ProcessName,
    WashProcessIssueId,
    IssueName,
    IssueQty
FROM Agg
ORDER BY IssueQty DESC;
";



        public const string GetWetSummary = @"
WITH BaseQc AS
(
    SELECT
        fdpq.Id,
        fdpq.WashBatchCardId,
        fdpq.WashProcessId,
        fdpq.QcStatusId,
		fdpq.Quantity,
        fdpq.CreateDate,
	
        -- Operational Date
        CASE
            WHEN CONVERT(TIME, fdpq.CreateDate) >= '08:00:00'
                THEN CONVERT(DATE, fdpq.CreateDate)
            ELSE CONVERT(DATE, DATEADD(DAY,-1,fdpq.CreateDate))
        END AS OperationalDate,

        -- Shift
        CASE
            WHEN CONVERT(TIME, fdpq.CreateDate) >= '08:00:00'
             AND CONVERT(TIME, fdpq.CreateDate) < '20:00:00'
            THEN 1
            ELSE 2
        END AS Shift

    FROM WashBatchCardQc fdpq

    WHERE
        fdpq.IsDeleted = 0
        AND fdpq.IsActive = 1

        AND
        (
            @FromDate IS NULL
            OR
            (
                CASE
                    WHEN CONVERT(TIME, fdpq.CreateDate) >= '08:00:00'
                        THEN CONVERT(DATE, fdpq.CreateDate)
                    ELSE CONVERT(DATE, DATEADD(DAY,-1,fdpq.CreateDate))
                END
            ) >= @FromDate
        )

        AND
        (
            @ToDate IS NULL
            OR
            (
                CASE
                    WHEN CONVERT(TIME, fdpq.CreateDate) >= '08:00:00'
                        THEN CONVERT(DATE, fdpq.CreateDate)
                    ELSE CONVERT(DATE, DATEADD(DAY,-1,fdpq.CreateDate))
                END
            ) <= @ToDate
        )
),

QcData AS
(
    SELECT
        b.WashProcessId,
        wp.ProcessName,

        fdp.ProcessModuleId,
        pm.Name AS ProcessModuleName,

        pu.PlantId,
        fdp.UnitId,

        b.OperationalDate,
        b.Shift,

        SUM(CASE WHEN b.QcStatusId IN (1,3) THEN Quantity ELSE 0 END) AS PassQty,
        SUM(CASE WHEN b.QcStatusId = 2 THEN Quantity ELSE 0 END) AS DefectQty,
        SUM(CASE WHEN b.QcStatusId = 4 THEN Quantity ELSE 0 END) AS RejectQty

    FROM BaseQc b

    JOIN WashBatchCard fdp
        ON b.WashBatchCardId = fdp.Id

    JOIN WashProcess wp
        ON b.WashProcessId = wp.Id

    JOIN ProcessModule pm
        ON fdp.ProcessModuleId = pm.Id

    JOIN PlantUnit pu
        ON fdp.UnitId = pu.Id

    WHERE
        (@PlantIds IS NULL
            OR pu.PlantId IN (
                SELECT TRY_CAST(value AS INT)
                FROM STRING_SPLIT(@PlantIds, ',')
            )
        )

        AND (@UnitIds IS NULL
            OR fdp.UnitId IN (
                SELECT TRY_CAST(value AS INT)
                FROM STRING_SPLIT(@UnitIds, ',')
            )
        )

        AND (@ProcessModuleIds IS NULL
            OR fdp.ProcessModuleId IN (
                SELECT TRY_CAST(value AS INT)
                FROM STRING_SPLIT(@ProcessModuleIds, ',')
            )
        )

        AND (@WashProcessIds IS NULL
            OR b.WashProcessId IN (
                SELECT TRY_CAST(value AS INT)
                FROM STRING_SPLIT(@WashProcessIds, ',')
            )
        )

        AND (@ShiftList IS NULL
            OR b.Shift IN (
                SELECT TRY_CAST(value AS INT)
                FROM STRING_SPLIT(@ShiftList, ',')
            )
        )

    GROUP BY
        b.WashProcessId,
        wp.ProcessName,
        fdp.ProcessModuleId,
        pm.Name,
        pu.PlantId,
        fdp.UnitId,
        b.OperationalDate,
        b.Shift
),

IssueData AS
(
    SELECT
        fdp.UnitId,
        fdp.ProcessModuleId,
        b.WashProcessId,

        b.OperationalDate,
        b.Shift,

        COUNT_BIG(*) AS IssueQty

    FROM WashBatchCardQcIsue qi

    JOIN BaseQc b
        ON qi.WashBatchCardQcId = b.Id

    JOIN WashBatchCard fdp
        ON b.WashBatchCardId = fdp.Id

    GROUP BY
        fdp.UnitId,
        fdp.ProcessModuleId,
        b.WashProcessId,
        b.OperationalDate,
        b.Shift
),

TargetData AS
(
    SELECT
        wh.WorkingHourDay AS OperationalDate,
        wh.UnitId,
        wp.ProcessModuleId,
        whdp.WashProcessId,

        CASE
            WHEN whd.StartTime >= '08:00:00'
             AND whd.EndTime < '20:00:00'
            THEN 1
            ELSE 2
        END AS Shift,

        SUM(whdp.DailyTarget) AS DayTarget,
        AVG(CAST(whdp.ManPower AS DECIMAL(18,2))) AS ManPower,
        AVG(CAST(whdp.SMV AS DECIMAL(18,2))) AS SMV

    FROM WorkingHourDetailManPower whdp

    JOIN WorkingHourDetail whd
        ON whdp.WorkingHourDetailId = whd.Id

    JOIN WorkingHour wh
        ON whd.WorkingHourId = wh.Id

    JOIN WashProcess wp
        ON whdp.WashProcessId = wp.Id

    WHERE
        whdp.IsActive = 1
        AND whdp.IsDeleted = 0

        AND (@FromDate IS NULL OR wh.WorkingHourDay >= @FromDate)
        AND (@ToDate IS NULL OR wh.WorkingHourDay <= @ToDate)

    GROUP BY
        wh.WorkingHourDay,
        wh.UnitId,
        wp.ProcessModuleId,
        whdp.WashProcessId,

        CASE
            WHEN whd.StartTime >= '08:00:00'
             AND whd.EndTime < '20:00:00'
            THEN 1
            ELSE 2
        END
)

SELECT
    q.ProcessModuleId,
    q.ProcessModuleName,

    q.WashProcessId,
    q.ProcessName,

    SUM(q.PassQty) AS PassQty,
    SUM(q.DefectQty) AS DefectQty,
    SUM(q.RejectQty) AS RejectQty,

    SUM(ISNULL(i.IssueQty,0)) AS IssueQty,

    SUM(ISNULL(t.DayTarget,0)) AS DayTarget,
    AVG(ISNULL(t.ManPower,0)) AS ManPower,
    AVG(ISNULL(t.SMV,0)) AS SMV,

    -- DHU
    CASE
        WHEN SUM(q.PassQty) = 0 THEN 0
        ELSE CAST(
            (SUM(ISNULL(i.IssueQty,0)) * 100.0)
            / SUM(q.PassQty)
        AS DECIMAL(18,2))
    END AS DHU,

    -- PlanEff
    CASE
        WHEN AVG(ISNULL(t.ManPower,0)) = 0
          OR AVG(ISNULL(t.SMV,0)) = 0
        THEN 0
        ELSE CAST(
            (SUM(ISNULL(t.DayTarget,0))
            * AVG(ISNULL(t.SMV,0))
            * 100.0)
            /
            (11 * AVG(ISNULL(t.ManPower,0)) * 60)
        AS DECIMAL(18,2))
    END AS PlanEff,

    -- ActualEff
    CASE
        WHEN AVG(ISNULL(t.ManPower,0)) = 0
          OR AVG(ISNULL(t.SMV,0)) = 0
        THEN 0
        ELSE CAST(
            (SUM(q.PassQty)
            * AVG(ISNULL(t.SMV,0))
            * 100.0)
            /
            (11 * AVG(ISNULL(t.ManPower,0)) * 60)
        AS DECIMAL(18,2))
    END AS ActualEff

FROM QcData q

LEFT JOIN IssueData i
    ON q.UnitId = i.UnitId
    AND q.ProcessModuleId = i.ProcessModuleId
    AND q.WashProcessId = i.WashProcessId
    AND q.OperationalDate = i.OperationalDate
    AND q.Shift = i.Shift

LEFT JOIN TargetData t
    ON q.UnitId = t.UnitId
    AND q.ProcessModuleId = t.ProcessModuleId
    AND q.WashProcessId = t.WashProcessId
    AND q.OperationalDate = t.OperationalDate
    AND q.Shift = t.Shift

GROUP BY
    q.ProcessModuleId,
    q.ProcessModuleName,
    q.WashProcessId,
    q.ProcessName

ORDER BY
    q.ProcessModuleName,
    q.ProcessName;
";


        public const string GetWetTopIssues = @"
WITH BaseFiltered AS
(
    SELECT
        fdpqi.WashBatchCardQcId,
        fdpqi.WashProcessIssueId,

        fdq.WashBatchCardId,
        fdq.WashProcessId,
        fdq.CreateDate,

        pm.Id AS ProcessModuleId,
        pm.Name AS ProcessModuleName,

        wp.ProcessName,
        wp.UnitId,

        wpi.IssueName,

        CASE
            WHEN CONVERT(TIME, fdq.CreateDate) >= '08:00:00'
                THEN CONVERT(DATE, fdq.CreateDate)
            ELSE CONVERT(DATE, DATEADD(DAY,-1,fdq.CreateDate))
        END AS OperationalDate,

        CASE
            WHEN CONVERT(TIME, fdq.CreateDate) >= '08:00:00'
             AND CONVERT(TIME, fdq.CreateDate) < '20:00:00'
            THEN 1 ELSE 2
        END AS Shift

    FROM WashBatchCardQcIsue fdpqi

    JOIN WashBatchCardQc fdq
        ON fdpqi.WashBatchCardQcId = fdq.Id

    JOIN WashBatchCard fdp
        ON fdq.WashBatchCardId = fdp.Id

    JOIN ProcessModule pm
        ON fdp.ProcessModuleId = pm.Id

    JOIN WashProcess wp
        ON fdq.WashProcessId = wp.Id

    JOIN WashProcessIssue wpi
        ON fdpqi.WashProcessIssueId = wpi.Id

    JOIN PlantUnit pu
        ON fdp.UnitId = pu.Id

    WHERE
        fdpqi.IsDeleted = 0
        AND fdpqi.IsActive = 1
        AND fdq.IsDeleted = 0
        AND fdq.IsActive = 1

        AND (
            @FromDate IS NULL OR @ToDate IS NULL
            OR CONVERT(DATE, fdq.CreateDate)
            BETWEEN @FromDate AND @ToDate
        )

        AND (
            @PlantIds IS NULL
            OR pu.PlantId IN (SELECT TRY_CAST(value AS INT) FROM STRING_SPLIT(@PlantIds, ','))
        )

        AND (
            @UnitIds IS NULL
            OR fdp.UnitId IN (SELECT TRY_CAST(value AS INT) FROM STRING_SPLIT(@UnitIds, ','))
        )

        AND (
            @ProcessModuleIds IS NULL
            OR pm.Id IN (SELECT TRY_CAST(value AS INT) FROM STRING_SPLIT(@ProcessModuleIds, ','))
        )

        AND (
            @WashProcessIds IS NULL
            OR wp.Id IN (SELECT TRY_CAST(value AS INT) FROM STRING_SPLIT(@WashProcessIds, ','))
        )

        AND (
            @ShiftList IS NULL
            OR (
                CASE
                    WHEN CONVERT(TIME, fdq.CreateDate) >= '08:00:00'
                     AND CONVERT(TIME, fdq.CreateDate) < '20:00:00'
                    THEN 1 ELSE 2
                END
            ) IN (SELECT TRY_CAST(value AS INT) FROM STRING_SPLIT(@ShiftList, ','))
        )
),

Agg AS
(
    SELECT
        ProcessModuleId,
        ProcessModuleName,
        WashProcessId,
        ProcessName,
        WashProcessIssueId,
        IssueName,
        COUNT_BIG(*) AS IssueQty
    FROM BaseFiltered
    GROUP BY
        ProcessModuleId,
        ProcessModuleName,
        WashProcessId,
        ProcessName,
        WashProcessIssueId,
        IssueName
)

SELECT TOP 5
    ProcessModuleId,
    ProcessModuleName,
    WashProcessId,
    ProcessName,
    WashProcessIssueId,
    IssueName,
    IssueQty
FROM Agg
ORDER BY IssueQty DESC;
";
    }
}
