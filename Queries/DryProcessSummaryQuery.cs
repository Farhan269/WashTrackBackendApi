namespace wsahRecieveDelivary.Queries
{
    public static class DryProcessSummaryQuery
    {
        public const string GetSummary = @"
WITH QcBase AS
(
    SELECT 
        pu.PlantId,
        p.PlantName,

        fdp.UnitId,
        pu.UnitName,

        fdp.ProcessModuleId,
        pm.Name AS ProcessModuleName,

        fdpq.WashProcessId,
        wp.ProcessName,

        CASE
            WHEN CAST(fdpq.CreateDate AS TIME) >= '08:00:00'
                THEN CAST(fdpq.CreateDate AS DATE)
            ELSE DATEADD(DAY,-1,CAST(fdpq.CreateDate AS DATE))
        END AS OperationalDate,

        CASE
            WHEN CAST(fdpq.CreateDate AS TIME) >= '08:00:00'
             AND CAST(fdpq.CreateDate AS TIME) < '20:00:00'
            THEN 1 ELSE 2
        END AS Shift,

        CASE WHEN fdpq.QcStatusId IN (1,3) THEN 1 ELSE 0 END AS PassQty,
        CASE WHEN fdpq.QcStatusId = 2 THEN 1 ELSE 0 END AS DefectQty,
        CASE WHEN fdpq.QcStatusId = 4 THEN 1 ELSE 0 END AS RejectQty

    FROM FirstDryProcessQc fdpq
    JOIN FirstDryProcess fdp ON fdpq.FirstDryProcessId = fdp.Id
    JOIN ProcessModule pm ON fdp.ProcessModuleId = pm.Id
    JOIN WashProcess wp ON fdpq.WashProcessId = wp.Id
    JOIN PlantUnit pu ON fdp.UnitId = pu.Id
    JOIN Plant p ON pu.PlantId = p.Id

    WHERE fdpq.IsDeleted = 0 AND fdpq.IsActive = 1
),

QcAgg AS
(
    SELECT
        PlantId, PlantName,
        UnitId, UnitName,
        ProcessModuleId, ProcessModuleName,
        WashProcessId, ProcessName,
        OperationalDate, Shift,

        SUM(PassQty) AS PassQty,
        SUM(DefectQty) AS DefectQty,
        SUM(RejectQty) AS RejectQty
    FROM QcBase
    GROUP BY
        PlantId, PlantName,
        UnitId, UnitName,
        ProcessModuleId, ProcessModuleName,
        WashProcessId, ProcessName,
        OperationalDate, Shift
),

IssueAgg AS
(
    SELECT
        fdp.UnitId,
        fdp.ProcessModuleId,
        fdpq.WashProcessId,

        CASE
            WHEN CAST(fdpq.CreateDate AS TIME) >= '08:00:00'
                THEN CAST(fdpq.CreateDate AS DATE)
            ELSE DATEADD(DAY,-1,CAST(fdpq.CreateDate AS DATE))
        END AS OperationalDate,

        CASE
            WHEN CAST(fdpq.CreateDate AS TIME) >= '08:00:00'
             AND CAST(fdpq.CreateDate AS TIME) < '20:00:00'
            THEN 1 ELSE 2
        END AS Shift,

        COUNT(*) AS IssueQty
    FROM FirstDryProcessQcIsuee fdpqi
    JOIN FirstDryProcessQc fdpq ON fdpqi.FirstDryProcessQcId = fdpq.Id
    JOIN FirstDryProcess fdp ON fdpq.FirstDryProcessId = fdp.Id
    WHERE fdpq.IsDeleted = 0 AND fdpq.IsActive = 1
    GROUP BY
        fdp.UnitId,
        fdp.ProcessModuleId,
        fdpq.WashProcessId,
        CASE
            WHEN CAST(fdpq.CreateDate AS TIME) >= '08:00:00'
                THEN CAST(fdpq.CreateDate AS DATE)
            ELSE DATEADD(DAY,-1,CAST(fdpq.CreateDate AS DATE))
        END,
        CASE
            WHEN CAST(fdpq.CreateDate AS TIME) >= '08:00:00'
             AND CAST(fdpq.CreateDate AS TIME) < '20:00:00'
            THEN 1 ELSE 2
        END
),

TargetAgg AS
(
    SELECT
        wh.UnitId,
        wp.ProcessModuleId,
        whdp.WashProcessId,
        wh.WorkingHourDay AS OperationalDate,

        CASE
            WHEN whd.StartTime >= '08:00:00'
             AND whd.EndTime <= '19:59:00'
            THEN 1 ELSE 2
        END AS Shift,

        SUM(whdp.DailyTarget) AS DayTarget,
        AVG(CAST(whdp.ManPower AS DECIMAL(18,2))) AS ManPower,
        AVG(CAST(whdp.SMV AS DECIMAL(18,2))) AS SMV
    FROM WorkingHourDetailManPower whdp
    JOIN WorkingHourDetail whd ON whdp.WorkingHourDetailId = whd.Id
    JOIN WorkingHour wh ON whd.WorkingHourId = wh.Id
    JOIN WashProcess wp ON whdp.WashProcessId = wp.Id
    WHERE whdp.IsActive = 1 AND whdp.IsDeleted = 0
    GROUP BY
        wh.UnitId,
        wp.ProcessModuleId,
        whdp.WashProcessId,
        wh.WorkingHourDay,
        CASE
            WHEN whd.StartTime >= '08:00:00'
             AND whd.EndTime <= '19:59:00'
            THEN 1 ELSE 2
        END
)

SELECT
    q.PlantId,
    q.PlantName,
    q.UnitId,
    q.UnitName,
    q.ProcessModuleId,
    q.ProcessModuleName,
    q.WashProcessId,
    q.ProcessName,
    q.OperationalDate,
    q.Shift,

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
        ELSE (SUM(ISNULL(i.IssueQty,0)) * 100.0) / SUM(q.PassQty)
    END AS DHU,

    -- Plan Efficiency
    CASE
        WHEN AVG(ISNULL(t.ManPower,0)) = 0 OR AVG(ISNULL(t.SMV,0)) = 0 THEN 0
        ELSE (SUM(ISNULL(t.DayTarget,0)) * AVG(ISNULL(t.SMV,0)) * 100.0)
             / (11 * AVG(ISNULL(t.ManPower,0)) * 60)
    END AS PlanEff,

    -- Actual Efficiency
    CASE
        WHEN AVG(ISNULL(t.ManPower,0)) = 0 OR AVG(ISNULL(t.SMV,0)) = 0 THEN 0
        ELSE (SUM(q.PassQty) * AVG(ISNULL(t.SMV,0)) * 100.0)
             / (11 * AVG(ISNULL(t.ManPower,0)) * 60)
    END AS ActualEff

FROM QcAgg q
LEFT JOIN IssueAgg i
    ON q.UnitId = i.UnitId
    AND q.ProcessModuleId = i.ProcessModuleId
    AND q.WashProcessId = i.WashProcessId
    AND q.OperationalDate = i.OperationalDate
    AND q.Shift = i.Shift

LEFT JOIN TargetAgg t
    ON q.UnitId = t.UnitId
    AND q.ProcessModuleId = t.ProcessModuleId
    AND q.WashProcessId = t.WashProcessId
    AND q.OperationalDate = t.OperationalDate
    AND q.Shift = t.Shift

GROUP BY
    q.PlantId,
    q.PlantName,
    q.UnitId,
    q.UnitName,
    q.ProcessModuleId,
    q.ProcessModuleName,
    q.WashProcessId,
    q.ProcessName,
    q.OperationalDate,
    q.Shift
ORDER BY
    q.OperationalDate, q.PlantName;
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
WITH QcData AS
(
    SELECT 
        fdpq.WashProcessId,
        wp.ProcessName,

        fdp.ProcessModuleId,
        pm.Name AS ProcessModuleName,

        pu.PlantId,
        fdp.UnitId,

        CASE
            WHEN CAST(fdpq.CreateDate AS TIME) >= '08:00:00'
                THEN CAST(fdpq.CreateDate AS DATE)
            ELSE CAST(DATEADD(DAY,-1,fdpq.CreateDate) AS DATE)
        END AS OperationalDate,

        CASE
            WHEN CAST(fdpq.CreateDate AS TIME) >= '08:00:00'
             AND CAST(fdpq.CreateDate AS TIME) < '20:00:00'
            THEN 1 ELSE 2
        END AS Shift,

        SUM(CASE WHEN fdpq.QcStatusId IN (1,3) THEN Quantity ELSE 0 END) AS PassQty,
        SUM(CASE WHEN fdpq.QcStatusId = 2 THEN Quantity ELSE 0 END) AS DefectQty,
        SUM(CASE WHEN fdpq.QcStatusId = 4 THEN Quantity ELSE 0 END) AS RejectQty

    FROM WashBatchCardQc fdpq
    JOIN WashProcess wp ON fdpq.WashProcessId = wp.Id
    JOIN WashBatchCard fdp ON fdpq.WashBatchCardId = fdp.Id
    JOIN ProcessModule pm ON fdp.ProcessModuleId = pm.Id
    JOIN PlantUnit pu ON fdp.UnitId = pu.Id

    WHERE 
        fdpq.IsDeleted = 0
        AND fdpq.IsActive = 1

        AND (
            CASE
                WHEN CAST(fdpq.CreateDate AS TIME) >= '08:00:00'
                    THEN CAST(fdpq.CreateDate AS DATE)
                ELSE CAST(DATEADD(DAY,-1,fdpq.CreateDate) AS DATE)
            END
        ) BETWEEN @FromDate AND @ToDate

        AND (@PlantIds IS NULL OR pu.PlantId IN (SELECT TRY_CAST(value AS INT) FROM STRING_SPLIT(@PlantIds, ',')))
        AND (@UnitIds IS NULL OR fdp.UnitId IN (SELECT TRY_CAST(value AS INT) FROM STRING_SPLIT(@UnitIds, ',')))
        AND (@ProcessModuleIds IS NULL OR fdp.ProcessModuleId IN (SELECT TRY_CAST(value AS INT) FROM STRING_SPLIT(@ProcessModuleIds, ',')))
        AND (@WashProcessIds IS NULL OR fdpq.WashProcessId IN (SELECT TRY_CAST(value AS INT) FROM STRING_SPLIT(@WashProcessIds, ',')))

    GROUP BY
        fdpq.WashProcessId,
        wp.ProcessName,
        fdp.ProcessModuleId,
        pm.Name,
        pu.PlantId,
        fdp.UnitId,

        CASE
            WHEN CAST(fdpq.CreateDate AS TIME) >= '08:00:00'
                THEN CAST(fdpq.CreateDate AS DATE)
            ELSE CAST(DATEADD(DAY,-1,fdpq.CreateDate) AS DATE)
        END,

        CASE
            WHEN CAST(fdpq.CreateDate AS TIME) >= '08:00:00'
             AND CAST(fdpq.CreateDate AS TIME) < '20:00:00'
            THEN 1 ELSE 2
        END
),

IssueData AS
(
    SELECT  
        fdp.UnitId,
        fdp.ProcessModuleId,
        fdpq.WashProcessId,

        CASE
            WHEN CAST(fdpq.CreateDate AS TIME) >= '08:00:00'
                THEN CAST(fdpq.CreateDate AS DATE)
            ELSE CAST(DATEADD(DAY,-1,fdpq.CreateDate) AS DATE)
        END AS OperationalDate,

        CASE
            WHEN CAST(fdpq.CreateDate AS TIME) >= '08:00:00'
             AND CAST(fdpq.CreateDate AS TIME) < '20:00:00'
            THEN 1 ELSE 2
        END AS Shift,

        COUNT(*) AS IssueQty

    FROM WashBatchCardQcIsue fdpqi
    JOIN WashBatchCardQc fdpq ON fdpqi.WashBatchCardQcId = fdpq.Id
    JOIN WashBatchCard fdp ON fdpq.WashBatchCardId = fdp.Id

    WHERE fdpq.IsDeleted = 0 AND fdpq.IsActive = 1

    GROUP BY
        fdp.UnitId,
        fdp.ProcessModuleId,
        fdpq.WashProcessId,
        CASE
            WHEN CAST(fdpq.CreateDate AS TIME) >= '08:00:00'
                THEN CAST(fdpq.CreateDate AS DATE)
            ELSE CAST(DATEADD(DAY,-1,fdpq.CreateDate) AS DATE)
        END,
        CASE
            WHEN CAST(fdpq.CreateDate AS TIME) >= '08:00:00'
             AND CAST(fdpq.CreateDate AS TIME) < '20:00:00'
            THEN 1 ELSE 2
        END
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
             AND whd.EndTime <= '19:59:00'
            THEN 1 ELSE 2
        END AS Shift,

        SUM(whdp.DailyTarget) AS DayTarget,
        AVG(CAST(whdp.ManPower AS DECIMAL(18,2))) AS ManPower,
        AVG(CAST(whdp.SMV AS DECIMAL(18,2))) AS SMV

    FROM WorkingHourDetailManPower whdp
    JOIN WorkingHourDetail whd ON whdp.WorkingHourDetailId = whd.Id
    JOIN WorkingHour wh ON whd.WorkingHourId = wh.Id
    JOIN WashProcess wp ON whdp.WashProcessId = wp.Id

    WHERE 
        whdp.IsActive = 1
        AND whdp.IsDeleted = 0
        AND wh.WorkingHourDay BETWEEN @FromDate AND @ToDate

    GROUP BY 
        wh.WorkingHourDay,
        wh.UnitId,
        wp.ProcessModuleId,
        whdp.WashProcessId,
        CASE
            WHEN whd.StartTime >= '08:00:00'
             AND whd.EndTime <= '19:59:00'
            THEN 1 ELSE 2
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
    SUM(ISNULL(t.DayTarget,0)) AS TargetQty,

    AVG(ISNULL(t.ManPower,0)) AS ManPower,
    AVG(ISNULL(t.SMV,0)) AS SMV,

    -- DHU
    CASE 
        WHEN SUM(q.PassQty) = 0 THEN 0
        ELSE CAST(
            (SUM(ISNULL(i.IssueQty,0)) * 100.0) /
            NULLIF(SUM(q.PassQty),0)
        AS DECIMAL(18,2))
    END AS DHU,

    -- Plan Efficiency
    CASE
        WHEN AVG(ISNULL(t.ManPower,0)) = 0 OR AVG(ISNULL(t.SMV,0)) = 0 THEN 0
        ELSE CAST(
            (SUM(ISNULL(t.DayTarget,0)) * AVG(ISNULL(t.SMV,0)) * 100.0)
            / (11 * AVG(ISNULL(t.ManPower,0)) * 60)
        AS DECIMAL(18,2))
    END AS PlanEff,

    -- Actual Efficiency
    CASE
        WHEN AVG(ISNULL(t.ManPower,0)) = 0 OR AVG(ISNULL(t.SMV,0)) = 0 THEN 0
        ELSE CAST(
            (SUM(q.PassQty) * AVG(ISNULL(t.SMV,0)) * 100.0)
            / (11 * AVG(ISNULL(t.ManPower,0)) * 60)
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

    FROM WashBatchCardQcIsue fdpqi

    JOIN WashBatchCardQc fdq
        ON fdpqi.WashBatchCardQcId = fdq.Id

    JOIN WashBatchCard fdp
        ON fdq.WashBatchCardId = fdp.Id

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
    }
}
