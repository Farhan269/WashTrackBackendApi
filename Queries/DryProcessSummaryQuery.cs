namespace wsahRecieveDelivary.Queries
{
    public static class DryProcessSummaryQuery
    {
        public const string GetSummary = @"
WITH PlantFilter AS
(
    SELECT TRY_CAST(value AS INT) AS Id
    FROM STRING_SPLIT(@PlantIds, ',')
    WHERE @PlantIds IS NOT NULL
),

UnitFilter AS
(
    SELECT TRY_CAST(value AS INT) AS Id
    FROM STRING_SPLIT(@UnitIds, ',')
    WHERE @UnitIds IS NOT NULL
),

ProcessModuleFilter AS
(
    SELECT TRY_CAST(value AS INT) AS Id
    FROM STRING_SPLIT(@ProcessModuleIds, ',')
    WHERE @ProcessModuleIds IS NOT NULL
),

WashProcessFilter AS
(
    SELECT TRY_CAST(value AS INT) AS Id
    FROM STRING_SPLIT(@WashProcessIds, ',')
    WHERE @WashProcessIds IS NOT NULL
),

ShiftFilter AS
(
    SELECT TRY_CAST(value AS INT) AS Id
    FROM STRING_SPLIT(@ShiftList, ',')
    WHERE @ShiftList IS NOT NULL
),

BaseQc AS
(
    SELECT
        fdpq.Id,
        fdpq.FirstDryProcessId,
        fdpq.WashProcessId,
        fdpq.QcStatusId,
        fdpq.CreateDate,

        CASE
            WHEN CAST(fdpq.CreateDate AS TIME) >= '08:00:00'
                THEN CAST(fdpq.CreateDate AS DATE)
            ELSE CAST(DATEADD(DAY, -1, fdpq.CreateDate) AS DATE)
        END AS OperationalDate,

        CASE
            WHEN CAST(fdpq.CreateDate AS TIME) >= '08:00:00'
             AND CAST(fdpq.CreateDate AS TIME) < '20:00:00'
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
            OR fdpq.CreateDate >= DATEADD(HOUR, 8, CAST(@FromDate AS DATETIME))
        )

        AND
        (
            @ToDate IS NULL
            OR fdpq.CreateDate < DATEADD(HOUR, 8, DATEADD(DAY, 1, CAST(@ToDate AS DATETIME)))
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

    INNER JOIN FirstDryProcess fdp
        ON b.FirstDryProcessId = fdp.Id

    INNER JOIN WashProcess wp
        ON b.WashProcessId = wp.Id

    INNER JOIN ProcessModule pm
        ON fdp.ProcessModuleId = pm.Id

    INNER JOIN PlantUnit pu
        ON fdp.UnitId = pu.Id

    WHERE
        (
            @PlantIds IS NULL
            OR EXISTS 
            (
                SELECT 1 
                FROM PlantFilter pf 
                WHERE pf.Id = pu.PlantId
            )
        )

        AND
        (
            @UnitIds IS NULL
            OR EXISTS 
            (
                SELECT 1 
                FROM UnitFilter uf 
                WHERE uf.Id = fdp.UnitId
            )
        )

        AND
        (
            @ProcessModuleIds IS NULL
            OR EXISTS 
            (
                SELECT 1 
                FROM ProcessModuleFilter pmf 
                WHERE pmf.Id = fdp.ProcessModuleId
            )
        )

        AND
        (
            @WashProcessIds IS NULL
            OR EXISTS 
            (
                SELECT 1 
                FROM WashProcessFilter wpf 
                WHERE wpf.Id = b.WashProcessId
            )
        )

        AND
        (
            @ShiftList IS NULL
            OR EXISTS 
            (
                SELECT 1 
                FROM ShiftFilter sf 
                WHERE sf.Id = b.Shift
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

    INNER JOIN BaseQc b
        ON qi.FirstDryProcessQcId = b.Id

    INNER JOIN FirstDryProcess fdp
        ON b.FirstDryProcessId = fdp.Id

    INNER JOIN PlantUnit pu
        ON fdp.UnitId = pu.Id

    WHERE
        (
            @PlantIds IS NULL
            OR EXISTS 
            (
                SELECT 1 
                FROM PlantFilter pf 
                WHERE pf.Id = pu.PlantId
            )
        )

        AND
        (
            @UnitIds IS NULL
            OR EXISTS 
            (
                SELECT 1 
                FROM UnitFilter uf 
                WHERE uf.Id = fdp.UnitId
            )
        )

        AND
        (
            @ProcessModuleIds IS NULL
            OR EXISTS 
            (
                SELECT 1 
                FROM ProcessModuleFilter pmf 
                WHERE pmf.Id = fdp.ProcessModuleId
            )
        )

        AND
        (
            @WashProcessIds IS NULL
            OR EXISTS 
            (
                SELECT 1 
                FROM WashProcessFilter wpf 
                WHERE wpf.Id = b.WashProcessId
            )
        )

        AND
        (
            @ShiftList IS NULL
            OR EXISTS 
            (
                SELECT 1 
                FROM ShiftFilter sf 
                WHERE sf.Id = b.Shift
            )
        )

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
        ROUND(AVG(CAST(whdp.ManPower AS DECIMAL(18,2))), 0) AS ManPower,
        AVG(CAST(whdp.SMV AS DECIMAL(18,2))) AS SMV

    FROM WorkingHourDetailManPower whdp

    INNER JOIN WorkingHourDetail whd
        ON whdp.WorkingHourDetailId = whd.Id

    INNER JOIN WorkingHour wh
        ON whd.WorkingHourId = wh.Id

    INNER JOIN WashProcess wp
        ON whdp.WashProcessId = wp.Id

    INNER JOIN PlantUnit pu
        ON wh.UnitId = pu.Id

    WHERE
        whdp.IsActive = 1
        AND whdp.IsDeleted = 0

        AND (@FromDate IS NULL OR wh.WorkingHourDay >= @FromDate)
        AND (@ToDate IS NULL OR wh.WorkingHourDay <= @ToDate)

        AND
        (
            @PlantIds IS NULL
            OR EXISTS 
            (
                SELECT 1 
                FROM PlantFilter pf 
                WHERE pf.Id = pu.PlantId
            )
        )

        AND
        (
            @UnitIds IS NULL
            OR EXISTS 
            (
                SELECT 1 
                FROM UnitFilter uf 
                WHERE uf.Id = wh.UnitId
            )
        )

        AND
        (
            @ProcessModuleIds IS NULL
            OR EXISTS 
            (
                SELECT 1 
                FROM ProcessModuleFilter pmf 
                WHERE pmf.Id = wp.ProcessModuleId
            )
        )

        AND
        (
            @WashProcessIds IS NULL
            OR EXISTS 
            (
                SELECT 1 
                FROM WashProcessFilter wpf 
                WHERE wpf.Id = whdp.WashProcessId
            )
        )

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
    ROUND(AVG(ISNULL(t.ManPower,0)), 0) AS ManPower,
    AVG(ISNULL(t.SMV,0)) AS SMV,

    CASE
        WHEN SUM(q.PassQty) = 0 THEN 0
        ELSE CAST(
            SUM(ISNULL(i.IssueQty,0)) * 100.0 / SUM(q.PassQty)
        AS DECIMAL(18,2))
    END AS DHU,

    CASE
        WHEN AVG(ISNULL(t.ManPower,0)) = 0
          OR AVG(ISNULL(t.SMV,0)) = 0
        THEN 0
        ELSE CAST(
            SUM(ISNULL(t.DayTarget,0))
            * AVG(ISNULL(t.SMV,0))
            * 100.0
            /
            (11 * AVG(ISNULL(t.ManPower,0)) * 60)
        AS DECIMAL(18,2))
    END AS PlanEff,

    CASE
        WHEN AVG(ISNULL(t.ManPower,0)) = 0
          OR AVG(ISNULL(t.SMV,0)) = 0
        THEN 0
        ELSE CAST(
            SUM(q.PassQty)
            * AVG(ISNULL(t.SMV,0))
            * 100.0
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
WITH PlantFilter AS
(
    SELECT TRY_CAST(value AS INT) AS Id
    FROM STRING_SPLIT(@PlantIds, ',')
    WHERE @PlantIds IS NOT NULL
),
UnitFilter AS
(
    SELECT TRY_CAST(value AS INT) AS Id
    FROM STRING_SPLIT(@UnitIds, ',')
    WHERE @UnitIds IS NOT NULL
),
ProcessModuleFilter AS
(
    SELECT TRY_CAST(value AS INT) AS Id
    FROM STRING_SPLIT(@ProcessModuleIds, ',')
    WHERE @ProcessModuleIds IS NOT NULL
),
WashProcessFilter AS
(
    SELECT TRY_CAST(value AS INT) AS Id
    FROM STRING_SPLIT(@WashProcessIds, ',')
    WHERE @WashProcessIds IS NOT NULL
),
ShiftFilter AS
(
    SELECT TRY_CAST(value AS INT) AS Id
    FROM STRING_SPLIT(@ShiftList, ',')
    WHERE @ShiftList IS NOT NULL
),

Base AS
(
    SELECT
        pm.Id AS ProcessModuleId,
        pm.Name AS ProcessModuleName,

        wp.Id AS WashProcessId,
        wp.ProcessName,

        wpi.Id AS WashProcessIssueId,
        wpi.IssueName,

        pu.PlantId,
        fdp.UnitId,

        CASE
            WHEN CAST(fdq.CreateDate AS TIME) >= '08:00:00'
                THEN CAST(fdq.CreateDate AS DATE)
            ELSE CAST(DATEADD(DAY, -1, fdq.CreateDate) AS DATE)
        END AS OperationalDate,

        CASE
            WHEN CAST(fdq.CreateDate AS TIME) >= '08:00:00'
             AND CAST(fdq.CreateDate AS TIME) < '20:00:00'
            THEN 1 
            ELSE 2
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

        /* Faster operational date filter */
        AND
        (
            @FromDate IS NULL
            OR fdq.CreateDate >= DATEADD(HOUR, 8, CAST(@FromDate AS DATETIME))
        )

        AND
        (
            @ToDate IS NULL
            OR fdq.CreateDate < DATEADD(HOUR, 8, DATEADD(DAY, 1, CAST(@ToDate AS DATETIME)))
        )

        AND
        (
            @PlantIds IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM PlantFilter pf
                WHERE pf.Id = pu.PlantId
            )
        )

        AND
        (
            @UnitIds IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM UnitFilter uf
                WHERE uf.Id = fdp.UnitId
            )
        )

        AND
        (
            @ProcessModuleIds IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM ProcessModuleFilter pmf
                WHERE pmf.Id = pm.Id
            )
        )

        AND
        (
            @WashProcessIds IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM WashProcessFilter wpf
                WHERE wpf.Id = wp.Id
            )
        )

        AND
        (
            @ShiftList IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM ShiftFilter sf
                WHERE sf.Id =
                    CASE
                        WHEN CAST(fdq.CreateDate AS TIME) >= '08:00:00'
                         AND CAST(fdq.CreateDate AS TIME) < '20:00:00'
                        THEN 1 
                        ELSE 2
                    END
            )
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

ORDER BY 
    IssueQty DESC

OPTION (RECOMPILE);
";



        public const string GetWetSummary = @"
WITH PlantFilter AS
(
    SELECT TRY_CAST(value AS INT) AS Id
    FROM STRING_SPLIT(@PlantIds, ',')
    WHERE @PlantIds IS NOT NULL
),
UnitFilter AS
(
    SELECT TRY_CAST(value AS INT) AS Id
    FROM STRING_SPLIT(@UnitIds, ',')
    WHERE @UnitIds IS NOT NULL
),
ProcessModuleFilter AS
(
    SELECT TRY_CAST(value AS INT) AS Id
    FROM STRING_SPLIT(@ProcessModuleIds, ',')
    WHERE @ProcessModuleIds IS NOT NULL
),
WashProcessFilter AS
(
    SELECT TRY_CAST(value AS INT) AS Id
    FROM STRING_SPLIT(@WashProcessIds, ',')
    WHERE @WashProcessIds IS NOT NULL
),
ShiftFilter AS
(
    SELECT TRY_CAST(value AS INT) AS Id
    FROM STRING_SPLIT(@ShiftList, ',')
    WHERE @ShiftList IS NOT NULL
),

BaseQc AS
(
    SELECT
        fdpq.Id,
        fdpq.WashBatchCardId,
        fdpq.WashProcessId,
        fdpq.QcStatusId,
        fdpq.Quantity,
        fdpq.CreateDate,

        CASE
            WHEN CAST(fdpq.CreateDate AS TIME) >= '08:00:00'
                THEN CAST(fdpq.CreateDate AS DATE)
            ELSE CAST(DATEADD(DAY, -1, fdpq.CreateDate) AS DATE)
        END AS OperationalDate,

        CASE
            WHEN CAST(fdpq.CreateDate AS TIME) >= '08:00:00'
             AND CAST(fdpq.CreateDate AS TIME) < '20:00:00'
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
            OR fdpq.CreateDate >= DATEADD(HOUR, 8, CAST(@FromDate AS DATETIME))
        )

        AND
        (
            @ToDate IS NULL
            OR fdpq.CreateDate < DATEADD(HOUR, 8, DATEADD(DAY, 1, CAST(@ToDate AS DATETIME)))
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

        SUM(CASE WHEN b.QcStatusId IN (1,3) THEN b.Quantity ELSE 0 END) AS PassQty,
        SUM(CASE WHEN b.QcStatusId = 2 THEN b.Quantity ELSE 0 END) AS DefectQty,
        SUM(CASE WHEN b.QcStatusId = 4 THEN b.Quantity ELSE 0 END) AS RejectQty

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
        (
            @PlantIds IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM PlantFilter pf
                WHERE pf.Id = pu.PlantId
            )
        )

        AND
        (
            @UnitIds IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM UnitFilter uf
                WHERE uf.Id = fdp.UnitId
            )
        )

        AND
        (
            @ProcessModuleIds IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM ProcessModuleFilter pmf
                WHERE pmf.Id = fdp.ProcessModuleId
            )
        )

        AND
        (
            @WashProcessIds IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM WashProcessFilter wpf
                WHERE wpf.Id = b.WashProcessId
            )
        )

        AND
        (
            @ShiftList IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM ShiftFilter sf
                WHERE sf.Id = b.Shift
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

    JOIN PlantUnit pu
        ON fdp.UnitId = pu.Id

    WHERE
        (
            @PlantIds IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM PlantFilter pf
                WHERE pf.Id = pu.PlantId
            )
        )

        AND
        (
            @UnitIds IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM UnitFilter uf
                WHERE uf.Id = fdp.UnitId
            )
        )

        AND
        (
            @ProcessModuleIds IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM ProcessModuleFilter pmf
                WHERE pmf.Id = fdp.ProcessModuleId
            )
        )

        AND
        (
            @WashProcessIds IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM WashProcessFilter wpf
                WHERE wpf.Id = b.WashProcessId
            )
        )

        AND
        (
            @ShiftList IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM ShiftFilter sf
                WHERE sf.Id = b.Shift
            )
        )

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
        ROUND(AVG(CAST(whdp.ManPower AS DECIMAL(18,2))), 0) AS ManPower,
        AVG(CAST(whdp.SMV AS DECIMAL(18,2))) AS SMV

    FROM WorkingHourDetailManPower whdp

    JOIN WorkingHourDetail whd
        ON whdp.WorkingHourDetailId = whd.Id

    JOIN WorkingHour wh
        ON whd.WorkingHourId = wh.Id

    JOIN WashProcess wp
        ON whdp.WashProcessId = wp.Id

    JOIN PlantUnit pu
        ON wh.UnitId = pu.Id

    WHERE
        whdp.IsActive = 1
        AND whdp.IsDeleted = 0

        AND (@FromDate IS NULL OR wh.WorkingHourDay >= @FromDate)
        AND (@ToDate IS NULL OR wh.WorkingHourDay <= @ToDate)

        AND
        (
            @PlantIds IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM PlantFilter pf
                WHERE pf.Id = pu.PlantId
            )
        )

        AND
        (
            @UnitIds IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM UnitFilter uf
                WHERE uf.Id = wh.UnitId
            )
        )

        AND
        (
            @ProcessModuleIds IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM ProcessModuleFilter pmf
                WHERE pmf.Id = wp.ProcessModuleId
            )
        )

        AND
        (
            @WashProcessIds IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM WashProcessFilter wpf
                WHERE wpf.Id = whdp.WashProcessId
            )
        )

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
    ROUND(AVG(ISNULL(t.ManPower,0)), 0) AS ManPower,
    AVG(ISNULL(t.SMV,0)) AS SMV,

    CASE
        WHEN SUM(q.PassQty) = 0 THEN 0
        ELSE CAST(
            SUM(ISNULL(i.IssueQty,0)) * 100.0 / SUM(q.PassQty)
        AS DECIMAL(18,2))
    END AS DHU,

    CASE
        WHEN AVG(ISNULL(t.ManPower,0)) = 0
          OR AVG(ISNULL(t.SMV,0)) = 0
        THEN 0
        ELSE CAST(
            SUM(ISNULL(t.DayTarget,0))
            * AVG(ISNULL(t.SMV,0))
            * 100.0
            /
            (11 * AVG(ISNULL(t.ManPower,0)) * 60)
        AS DECIMAL(18,2))
    END AS PlanEff,

    CASE
        WHEN AVG(ISNULL(t.ManPower,0)) = 0
          OR AVG(ISNULL(t.SMV,0)) = 0
        THEN 0
        ELSE CAST(
            SUM(q.PassQty)
            * AVG(ISNULL(t.SMV,0))
            * 100.0
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
    q.ProcessName
OPTION (RECOMPILE);
";


        public const string GetWetTopIssues = @"
WITH PlantFilter AS
(
    SELECT TRY_CAST(value AS INT) AS Id
    FROM STRING_SPLIT(@PlantIds, ',')
    WHERE @PlantIds IS NOT NULL
),
UnitFilter AS
(
    SELECT TRY_CAST(value AS INT) AS Id
    FROM STRING_SPLIT(@UnitIds, ',')
    WHERE @UnitIds IS NOT NULL
),
ProcessModuleFilter AS
(
    SELECT TRY_CAST(value AS INT) AS Id
    FROM STRING_SPLIT(@ProcessModuleIds, ',')
    WHERE @ProcessModuleIds IS NOT NULL
),
WashProcessFilter AS
(
    SELECT TRY_CAST(value AS INT) AS Id
    FROM STRING_SPLIT(@WashProcessIds, ',')
    WHERE @WashProcessIds IS NOT NULL
),
ShiftFilter AS
(
    SELECT TRY_CAST(value AS INT) AS Id
    FROM STRING_SPLIT(@ShiftList, ',')
    WHERE @ShiftList IS NOT NULL
),

Base AS
(
    SELECT
        pm.Id AS ProcessModuleId,
        pm.Name AS ProcessModuleName,

        wp.Id AS WashProcessId,
        wp.ProcessName,

        wpi.Id AS WashProcessIssueId,
        wpi.IssueName,

        pu.PlantId,
        fdp.UnitId,

        CASE
            WHEN CAST(fdq.CreateDate AS TIME) >= '08:00:00'
                THEN CAST(fdq.CreateDate AS DATE)
            ELSE CAST(DATEADD(DAY, -1, fdq.CreateDate) AS DATE)
        END AS OperationalDate,

        CASE
            WHEN CAST(fdq.CreateDate AS TIME) >= '08:00:00'
             AND CAST(fdq.CreateDate AS TIME) < '20:00:00'
            THEN 1 
            ELSE 2
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

        /* faster date filtering */
        AND
        (
            @FromDate IS NULL
            OR fdq.CreateDate >= DATEADD(HOUR, 8, CAST(@FromDate AS DATETIME))
        )

        AND
        (
            @ToDate IS NULL
            OR fdq.CreateDate < DATEADD(HOUR, 8, DATEADD(DAY, 1, CAST(@ToDate AS DATETIME)))
        )

        AND
        (
            @PlantIds IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM PlantFilter pf
                WHERE pf.Id = pu.PlantId
            )
        )

        AND
        (
            @UnitIds IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM UnitFilter uf
                WHERE uf.Id = fdp.UnitId
            )
        )

        AND
        (
            @ProcessModuleIds IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM ProcessModuleFilter pmf
                WHERE pmf.Id = pm.Id
            )
        )

        AND
        (
            @WashProcessIds IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM WashProcessFilter wpf
                WHERE wpf.Id = wp.Id
            )
        )

        AND
        (
            @ShiftList IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM ShiftFilter sf
                WHERE sf.Id =
                    CASE
                        WHEN CAST(fdq.CreateDate AS TIME) >= '08:00:00'
                         AND CAST(fdq.CreateDate AS TIME) < '20:00:00'
                        THEN 1 
                        ELSE 2
                    END
            )
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

ORDER BY 
    IssueQty DESC

OPTION (RECOMPILE);
";
    }
}
