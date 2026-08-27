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

--SELECT TOP 5
  ----  ProcessModuleId,
  --  ProcessModuleName,

   --- WashProcessId,
  ---  ProcessName,

   --- WashProcessIssueId,
   --- IssueName,

  ---  IssueQty

---FROM Agg

----ORDER BY 
    ----IssueQty DESC

---OPTION (RECOMPILE);

, Ranked AS
(
    SELECT
        ProcessModuleId,
        ProcessModuleName,

        WashProcessId,
        ProcessName,

        WashProcessIssueId,
        IssueName,

        IssueQty,

        ROW_NUMBER() OVER
        (
            PARTITION BY WashProcessId
            ORDER BY IssueQty DESC
        ) AS RowNo
    FROM Agg
)

SELECT
    ProcessModuleId,
    ProcessModuleName,

    WashProcessId,
    ProcessName,

    WashProcessIssueId,
    IssueName,

    IssueQty

FROM Ranked
WHERE RowNo <= 3

ORDER BY
    ProcessName,
    IssueQty DESC

OPTION (RECOMPILE);
";



        public const string GetDetails = @"
;WITH PlantFilter AS
(
    SELECT DISTINCT
        TRY_CAST(LTRIM(RTRIM(value)) AS INT) AS Id
    FROM STRING_SPLIT(@PlantIds, ',')
    WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL
),

UnitFilter AS
(
    SELECT DISTINCT
        TRY_CAST(LTRIM(RTRIM(value)) AS INT) AS Id
    FROM STRING_SPLIT(@UnitIds, ',')
    WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL
),

ProcessModuleFilter AS
(
    SELECT DISTINCT
        TRY_CAST(LTRIM(RTRIM(value)) AS INT) AS Id
    FROM STRING_SPLIT(@ProcessModuleIds, ',')
    WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL
),

WashProcessFilter AS
(
    SELECT DISTINCT
        TRY_CAST(LTRIM(RTRIM(value)) AS INT) AS Id
    FROM STRING_SPLIT(@WashProcessIds, ',')
    WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL
),

ShiftFilter AS
(
    SELECT DISTINCT
        TRY_CAST(LTRIM(RTRIM(value)) AS INT) AS Id
    FROM STRING_SPLIT(@ShiftList, ',')
    WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL
),

BaseQc AS
(
    SELECT
        fdpq.Id,
        fdpq.WorkOrderId,

        wo.StyleName,
        wo.FastReactNo,
        wo.WorkOrderNo,

        fdpq.FirstDryProcessId,
        fdpq.WashProcessId,
        wp.ProcessName,

        fdp.ProcessModuleId,
        pm.Name AS ProcessModuleName,

        pu.PlantId,
        fdp.UnitId,

        fdpq.QcStatusId,
        fdpq.CreateDate,

        CASE
            WHEN CAST(fdpq.CreateDate AS TIME) >= '08:00:00'
                THEN CAST(fdpq.CreateDate AS DATE)
            ELSE
                CAST(
                    DATEADD(DAY, -1, fdpq.CreateDate)
                    AS DATE
                )
        END AS OperationalDate,

        CASE
            WHEN CAST(fdpq.CreateDate AS TIME) >= '08:00:00'
             AND CAST(fdpq.CreateDate AS TIME) < '20:00:00'
                THEN 1
            ELSE 2
        END AS Shift

    FROM FirstDryProcessQc fdpq

    INNER JOIN FirstDryProcess fdp
        ON fdp.Id = fdpq.FirstDryProcessId

    INNER JOIN PlantUnit pu
        ON pu.Id = fdp.UnitId

    INNER JOIN WashProcess wp
        ON wp.Id = fdpq.WashProcessId

    INNER JOIN ProcessModule pm
        ON pm.Id = fdp.ProcessModuleId

    LEFT JOIN WorkOrder wo
        ON wo.Id = fdpq.WorkOrderId

    WHERE
        fdpq.IsDeleted = 0
        AND fdpq.IsActive = 1

        AND
        (
            @FromDate IS NULL
            OR fdpq.CreateDate >= DATEADD(
                HOUR,
                8,
                CAST(@FromDate AS DATETIME)
            )
        )

        AND
        (
            @ToDate IS NULL
            OR fdpq.CreateDate < DATEADD(
                HOUR,
                8,
                DATEADD(
                    DAY,
                    1,
                    CAST(@ToDate AS DATETIME)
                )
            )
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
                WHERE wpf.Id = fdpq.WashProcessId
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
                        WHEN CAST(fdpq.CreateDate AS TIME) >= '08:00:00'
                         AND CAST(fdpq.CreateDate AS TIME) < '20:00:00'
                            THEN 1
                        ELSE 2
                    END
            )
        )

        AND
        (
            @SearchText IS NULL
            OR wo.StyleName LIKE '%' + @SearchText + '%'
            OR wo.FastReactNo LIKE '%' + @SearchText + '%'
            OR wo.WorkOrderNo LIKE '%' + @SearchText + '%'
        )
),

SelectedKeys AS
(
    SELECT DISTINCT
        WorkOrderId,
        UnitId,
        ProcessModuleId,
        WashProcessId
    FROM BaseQc
),

QcData AS
(
    SELECT
        b.WorkOrderId,
        b.StyleName,
        b.FastReactNo,
        b.WorkOrderNo,

        b.WashProcessId,
        b.ProcessName,

        b.ProcessModuleId,
        b.ProcessModuleName,

        b.PlantId,
        b.UnitId,

        b.OperationalDate,
        b.Shift,

        SUM(
            CASE
                WHEN b.QcStatusId IN (1, 3)
                    THEN 1
                ELSE 0
            END
        ) AS PassQty,

        SUM(
            CASE
                WHEN b.QcStatusId = 2
                    THEN 1
                ELSE 0
            END
        ) AS DefectQty,

        SUM(
            CASE
                WHEN b.QcStatusId = 4
                    THEN 1
                ELSE 0
            END
        ) AS RejectQty

    FROM BaseQc b

    GROUP BY
        b.WorkOrderId,
        b.StyleName,
        b.FastReactNo,
        b.WorkOrderNo,

        b.WashProcessId,
        b.ProcessName,

        b.ProcessModuleId,
        b.ProcessModuleName,

        b.PlantId,
        b.UnitId,

        b.OperationalDate,
        b.Shift
),

WholePassData AS
(
    SELECT
        fdpq.WorkOrderId,
        fdp.UnitId,
        fdp.ProcessModuleId,
        fdpq.WashProcessId,

        SUM(
            CASE
                WHEN fdpq.QcStatusId IN (1, 3)
                    THEN 1
                ELSE 0
            END
        ) AS TotalPassQty

    FROM FirstDryProcessQc fdpq

    INNER JOIN FirstDryProcess fdp
        ON fdp.Id = fdpq.FirstDryProcessId

    INNER JOIN SelectedKeys sk
        ON sk.WorkOrderId = fdpq.WorkOrderId
       AND sk.UnitId = fdp.UnitId
       AND sk.ProcessModuleId = fdp.ProcessModuleId
       AND sk.WashProcessId = fdpq.WashProcessId

    WHERE
        fdpq.IsDeleted = 0
        AND fdpq.IsActive = 1

       

    GROUP BY
        fdpq.WorkOrderId,
        fdp.UnitId,
        fdp.ProcessModuleId,
        fdpq.WashProcessId
),

IssueData AS
(
    SELECT
        b.WorkOrderId,
        b.UnitId,
        b.ProcessModuleId,
        b.WashProcessId,
        b.OperationalDate,
        b.Shift,

        COUNT_BIG(*) AS IssueQty

    FROM BaseQc b

    INNER JOIN FirstDryProcessQcIsuee qi
        ON qi.FirstDryProcessQcId = b.Id

    GROUP BY
        b.WorkOrderId,
        b.UnitId,
        b.ProcessModuleId,
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
             AND whd.StartTime < '20:00:00'
                THEN 1
            ELSE 2
        END AS Shift,

        SUM(whdp.DailyTarget) AS DayTarget,

        ROUND(
            AVG(
                CAST(
                    whdp.ManPower
                    AS DECIMAL(18, 2)
                )
            ),
            0
        ) AS ManPower,

        AVG(
            CAST(
                whdp.SMV
                AS DECIMAL(18, 2)
            )
        ) AS SMV

    FROM WorkingHourDetailManPower whdp

    INNER JOIN WorkingHourDetail whd
        ON whd.Id = whdp.WorkingHourDetailId

    INNER JOIN WorkingHour wh
        ON wh.Id = whd.WorkingHourId

    INNER JOIN WashProcess wp
        ON wp.Id = whdp.WashProcessId

    INNER JOIN PlantUnit pu
        ON pu.Id = wh.UnitId

    WHERE
        whdp.IsActive = 1
        AND whdp.IsDeleted = 0

        AND
        (
            @FromDate IS NULL
            OR wh.WorkingHourDay >= @FromDate
        )

        AND
        (
            @ToDate IS NULL
            OR wh.WorkingHourDay <= @ToDate
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

        AND
        (
            @ShiftList IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM ShiftFilter sf
                WHERE sf.Id =
                    CASE
                        WHEN whd.StartTime >= '08:00:00'
                         AND whd.StartTime < '20:00:00'
                            THEN 1
                        ELSE 2
                    END
            )
        )

    GROUP BY
        wh.WorkingHourDay,
        wh.UnitId,
        wp.ProcessModuleId,
        whdp.WashProcessId,

        CASE
            WHEN whd.StartTime >= '08:00:00'
             AND whd.StartTime < '20:00:00'
                THEN 1
            ELSE 2
        END
)

SELECT
    q.ProcessModuleId,
    q.ProcessModuleName,
    q.StyleName,
    q.FastReactNo,
    q.WorkOrderNo,

    q.WashProcessId,
    q.ProcessName,

    SUM(q.PassQty) AS PassQty,
    SUM(q.DefectQty) AS DefectQty,
    SUM(q.RejectQty) AS RejectQty,

    MAX(
        ISNULL(w.TotalPassQty, 0)
    ) AS TotalPassQty,

    SUM(
        ISNULL(i.IssueQty, 0)
    ) AS IssueQty,

    SUM(
        ISNULL(t.DayTarget, 0)
    ) AS DayTarget,

    ROUND(
        AVG(
            ISNULL(t.ManPower, 0)
        ),
        0
    ) AS ManPower,

    AVG(
        ISNULL(t.SMV, 0)
    ) AS SMV,

    CASE
        WHEN SUM(q.PassQty) = 0
            THEN 0
        ELSE CAST(
            SUM(ISNULL(i.IssueQty, 0))
            * 100.0
            / NULLIF(SUM(q.PassQty), 0)
            AS DECIMAL(18, 2)
        )
    END AS DHU,

    CASE
        WHEN AVG(ISNULL(t.ManPower, 0)) = 0
          OR AVG(ISNULL(t.SMV, 0)) = 0
            THEN 0
        ELSE CAST(
            SUM(ISNULL(t.DayTarget, 0))
            * AVG(ISNULL(t.SMV, 0))
            * 100.0
            /
            (
                11
                * AVG(ISNULL(t.ManPower, 0))
                * 60
            )
            AS DECIMAL(18, 2)
        )
    END AS PlanEff,

    CASE
        WHEN AVG(ISNULL(t.ManPower, 0)) = 0
          OR AVG(ISNULL(t.SMV, 0)) = 0
            THEN 0
        ELSE CAST(
            SUM(q.PassQty)
            * AVG(ISNULL(t.SMV, 0))
            * 100.0
            /
            (
                11
                * AVG(ISNULL(t.ManPower, 0))
                * 60
            )
            AS DECIMAL(18, 2)
        )
    END AS ActualEff

FROM QcData q

LEFT JOIN WholePassData w
    ON w.WorkOrderId = q.WorkOrderId
   AND w.UnitId = q.UnitId
   AND w.ProcessModuleId = q.ProcessModuleId
   AND w.WashProcessId = q.WashProcessId

LEFT JOIN IssueData i
    ON i.WorkOrderId = q.WorkOrderId
   AND i.UnitId = q.UnitId
   AND i.ProcessModuleId = q.ProcessModuleId
   AND i.WashProcessId = q.WashProcessId
   AND i.OperationalDate = q.OperationalDate
   AND i.Shift = q.Shift

LEFT JOIN TargetData t
    ON t.UnitId = q.UnitId
   AND t.ProcessModuleId = q.ProcessModuleId
   AND t.WashProcessId = q.WashProcessId
   AND t.OperationalDate = q.OperationalDate
   AND t.Shift = q.Shift

GROUP BY
    q.ProcessModuleId,
    q.ProcessModuleName,

    q.StyleName,
    q.FastReactNo,
    q.WorkOrderNo,

    q.WashProcessId,
    q.ProcessName

ORDER BY
    q.ProcessModuleName,
    q.StyleName,
    q.FastReactNo,
    q.WorkOrderNo,
    q.ProcessName

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


        public const string GetWetDetails = @"
;WITH PlantFilter AS
(
    SELECT DISTINCT
        TRY_CAST(LTRIM(RTRIM(value)) AS INT) AS Id
    FROM STRING_SPLIT(@PlantIds, ',')
    WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL
),

UnitFilter AS
(
    SELECT DISTINCT
        TRY_CAST(LTRIM(RTRIM(value)) AS INT) AS Id
    FROM STRING_SPLIT(@UnitIds, ',')
    WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL
),

ProcessModuleFilter AS
(
    SELECT DISTINCT
        TRY_CAST(LTRIM(RTRIM(value)) AS INT) AS Id
    FROM STRING_SPLIT(@ProcessModuleIds, ',')
    WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL
),

WashProcessFilter AS
(
    SELECT DISTINCT
        TRY_CAST(LTRIM(RTRIM(value)) AS INT) AS Id
    FROM STRING_SPLIT(@WashProcessIds, ',')
    WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL
),

ShiftFilter AS
(
    SELECT DISTINCT
        TRY_CAST(LTRIM(RTRIM(value)) AS INT) AS Id
    FROM STRING_SPLIT(@ShiftList, ',')
    WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL
),

BaseQc AS
(
    SELECT
        fdpq.Id,
        fdpq.WorkOrderId,
        ISNULL(fdpq.Quantity, 0) AS Quantity,

        wo.StyleName,
        wo.FastReactNo,
        wo.WorkOrderNo,

        fdpq.WashBatchCardId,
        fdpq.WashProcessId,
        wp.ProcessName,

        wbc.ProcessModuleId,
        pm.Name AS ProcessModuleName,

        pu.PlantId,
        wbc.UnitId,

        fdpq.QcStatusId,
        fdpq.CreateDate,

        CASE
            WHEN CAST(fdpq.CreateDate AS TIME) >= '08:00:00'
                THEN CAST(fdpq.CreateDate AS DATE)
            ELSE
                CAST(
                    DATEADD(DAY, -1, fdpq.CreateDate)
                    AS DATE
                )
        END AS OperationalDate,

        CASE
            WHEN CAST(fdpq.CreateDate AS TIME) >= '08:00:00'
             AND CAST(fdpq.CreateDate AS TIME) < '20:00:00'
                THEN 1
            ELSE 2
        END AS Shift

    FROM WashBatchCardQc fdpq

    INNER JOIN WashBatchCard wbc
        ON wbc.Id = fdpq.WashBatchCardId

    INNER JOIN PlantUnit pu
        ON pu.Id = wbc.UnitId

    INNER JOIN WashProcess wp
        ON wp.Id = fdpq.WashProcessId

    INNER JOIN ProcessModule pm
        ON pm.Id = wbc.ProcessModuleId

    LEFT JOIN WorkOrder wo
        ON wo.Id = fdpq.WorkOrderId

    WHERE
        fdpq.IsDeleted = 0
        AND fdpq.IsActive = 1

        ----------------------------------------------------
        -- Operational date range: 08:00 to next day 08:00
        ----------------------------------------------------
        AND
        (
            @FromDate IS NULL
            OR fdpq.CreateDate >= DATEADD(
                HOUR,
                8,
                CAST(@FromDate AS DATETIME)
            )
        )

        AND
        (
            @ToDate IS NULL
            OR fdpq.CreateDate < DATEADD(
                HOUR,
                8,
                DATEADD(
                    DAY,
                    1,
                    CAST(@ToDate AS DATETIME)
                )
            )
        )

        ----------------------------------------------------
        -- Plant filter
        ----------------------------------------------------
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

        ----------------------------------------------------
        -- Unit filter
        ----------------------------------------------------
        AND
        (
            @UnitIds IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM UnitFilter uf
                WHERE uf.Id = wbc.UnitId
            )
        )

        ----------------------------------------------------
        -- Process module filter
        ----------------------------------------------------
        AND
        (
            @ProcessModuleIds IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM ProcessModuleFilter pmf
                WHERE pmf.Id = wbc.ProcessModuleId
            )
        )

        ----------------------------------------------------
        -- Wash process filter
        ----------------------------------------------------
        AND
        (
            @WashProcessIds IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM WashProcessFilter wpf
                WHERE wpf.Id = fdpq.WashProcessId
            )
        )

        ----------------------------------------------------
        -- Shift filter
        ----------------------------------------------------
        AND
        (
            @ShiftList IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM ShiftFilter sf
                WHERE sf.Id =
                    CASE
                        WHEN CAST(fdpq.CreateDate AS TIME) >= '08:00:00'
                         AND CAST(fdpq.CreateDate AS TIME) < '20:00:00'
                            THEN 1
                        ELSE 2
                    END
            )
        )

        ----------------------------------------------------
        -- Search filter
        ----------------------------------------------------
        AND
        (
            @SearchText IS NULL
            OR LTRIM(RTRIM(@SearchText)) = ''
            OR wo.StyleName LIKE '%' + LTRIM(RTRIM(@SearchText)) + '%'
            OR wo.FastReactNo LIKE '%' + LTRIM(RTRIM(@SearchText)) + '%'
            OR wo.WorkOrderNo LIKE '%' + LTRIM(RTRIM(@SearchText)) + '%'
        )
),

SelectedKeys AS
(
    SELECT DISTINCT
        WorkOrderId,
        UnitId,
        ProcessModuleId,
        WashProcessId
    FROM BaseQc
),

QcData AS
(
    SELECT
        b.WorkOrderId,
        b.StyleName,
        b.FastReactNo,
        b.WorkOrderNo,

        b.WashProcessId,
        b.ProcessName,

        b.ProcessModuleId,
        b.ProcessModuleName,

        b.PlantId,
        b.UnitId,

        b.OperationalDate,
        b.Shift,

        SUM
        (
            CASE
                WHEN b.QcStatusId IN (1, 3)
                    THEN b.Quantity
                ELSE 0
            END
        ) AS PassQty,

        SUM
        (
            CASE
                WHEN b.QcStatusId = 2
                    THEN b.Quantity
                ELSE 0
            END
        ) AS DefectQty,

        SUM
        (
            CASE
                WHEN b.QcStatusId = 4
                    THEN b.Quantity
                ELSE 0
            END
        ) AS RejectQty

    FROM BaseQc b

    GROUP BY
        b.WorkOrderId,
        b.StyleName,
        b.FastReactNo,
        b.WorkOrderNo,

        b.WashProcessId,
        b.ProcessName,

        b.ProcessModuleId,
        b.ProcessModuleName,

        b.PlantId,
        b.UnitId,

        b.OperationalDate,
        b.Shift
),

WholePassData AS
(
    SELECT
        qc.WorkOrderId,
        wbc.UnitId,
        wbc.ProcessModuleId,
        qc.WashProcessId,

        SUM
        (
            CASE
                WHEN qc.QcStatusId IN (1, 3)
                    THEN ISNULL(qc.Quantity, 0)
                ELSE 0
            END
        ) AS TotalPassQty

    FROM WashBatchCardQc qc

    INNER JOIN WashBatchCard wbc
        ON wbc.Id = qc.WashBatchCardId

    INNER JOIN SelectedKeys sk
        ON sk.WorkOrderId = qc.WorkOrderId
       AND sk.UnitId = wbc.UnitId
       AND sk.ProcessModuleId = wbc.ProcessModuleId
       AND sk.WashProcessId = qc.WashProcessId

    WHERE
        qc.IsDeleted = 0
        AND qc.IsActive = 1

      
        
    GROUP BY
        qc.WorkOrderId,
        wbc.UnitId,
        wbc.ProcessModuleId,
        qc.WashProcessId
),

IssueData AS
(
    SELECT
        b.WorkOrderId,
        b.UnitId,
        b.ProcessModuleId,
        b.WashProcessId,
        b.OperationalDate,
        b.Shift,

        COUNT_BIG(*) AS IssueQty

    FROM BaseQc b

    INNER JOIN WashBatchCardQcIsue qi
        ON qi.WashBatchCardQcId = b.Id

    GROUP BY
        b.WorkOrderId,
        b.UnitId,
        b.ProcessModuleId,
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
             AND whd.StartTime < '20:00:00'
                THEN 1
            ELSE 2
        END AS Shift,

        SUM
        (
            ISNULL(whdp.DailyTarget, 0)
        ) AS DayTarget,

        ROUND
        (
            AVG
            (
                NULLIF
                (
                    CAST(
                        whdp.ManPower
                        AS DECIMAL(18, 2)
                    ),
                    0
                )
            ),
            0
        ) AS ManPower,

        AVG
        (
            NULLIF
            (
                CAST(
                    whdp.SMV
                    AS DECIMAL(18, 2)
                ),
                0
            )
        ) AS SMV

    FROM WorkingHourDetailManPower whdp

    INNER JOIN WorkingHourDetail whd
        ON whd.Id = whdp.WorkingHourDetailId

    INNER JOIN WorkingHour wh
        ON wh.Id = whd.WorkingHourId

    INNER JOIN WashProcess wp
        ON wp.Id = whdp.WashProcessId

    INNER JOIN PlantUnit pu
        ON pu.Id = wh.UnitId

    WHERE
        whdp.IsActive = 1
        AND whdp.IsDeleted = 0

        AND
        (
            @FromDate IS NULL
            OR wh.WorkingHourDay >= CAST(@FromDate AS DATE)
        )

        AND
        (
            @ToDate IS NULL
            OR wh.WorkingHourDay <= CAST(@ToDate AS DATE)
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

        AND
        (
            @ShiftList IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM ShiftFilter sf
                WHERE sf.Id =
                    CASE
                        WHEN whd.StartTime >= '08:00:00'
                         AND whd.StartTime < '20:00:00'
                            THEN 1
                        ELSE 2
                    END
            )
        )

    GROUP BY
        wh.WorkingHourDay,
        wh.UnitId,
        wp.ProcessModuleId,
        whdp.WashProcessId,

        CASE
            WHEN whd.StartTime >= '08:00:00'
             AND whd.StartTime < '20:00:00'
                THEN 1
            ELSE 2
        END
)

SELECT
    q.ProcessModuleId,
    q.ProcessModuleName,

    q.StyleName,
    q.FastReactNo,
    q.WorkOrderNo,

    q.WashProcessId,
    q.ProcessName,

    SUM(q.PassQty) AS PassQty,
    SUM(q.DefectQty) AS DefectQty,
    SUM(q.RejectQty) AS RejectQty,

    --------------------------------------------------------
    -- MAX prevents historical pass from repeating
    -- for every operational date and shift row
    --------------------------------------------------------
    MAX
    (
        ISNULL(w.TotalPassQty, 0)
    ) AS TotalPassQty,

    SUM
    (
        ISNULL(i.IssueQty, 0)
    ) AS IssueQty,

    SUM
    (
        ISNULL(t.DayTarget, 0)
    ) AS DayTarget,

    ROUND
    (
        AVG
        (
            NULLIF(t.ManPower, 0)
        ),
        0
    ) AS ManPower,

    AVG
    (
        NULLIF(t.SMV, 0)
    ) AS SMV,

    CASE
        WHEN SUM(q.PassQty) = 0
            THEN CAST(0 AS DECIMAL(18, 2))
        ELSE
            CAST
            (
                SUM(ISNULL(i.IssueQty, 0))
                * 100.0
                / NULLIF(SUM(q.PassQty), 0)
                AS DECIMAL(18, 2)
            )
    END AS DHU,

    CASE
        WHEN ISNULL
             (
                 AVG(NULLIF(t.ManPower, 0)),
                 0
             ) = 0
          OR ISNULL
             (
                 AVG(NULLIF(t.SMV, 0)),
                 0
             ) = 0
            THEN CAST(0 AS DECIMAL(18, 2))
        ELSE
            CAST
            (
                SUM(ISNULL(t.DayTarget, 0))
                * AVG(NULLIF(t.SMV, 0))
                * 100.0
                /
                (
                    11
                    * AVG(NULLIF(t.ManPower, 0))
                    * 60
                )
                AS DECIMAL(18, 2)
            )
    END AS PlanEff,

    CASE
        WHEN ISNULL
             (
                 AVG(NULLIF(t.ManPower, 0)),
                 0
             ) = 0
          OR ISNULL
             (
                 AVG(NULLIF(t.SMV, 0)),
                 0
             ) = 0
            THEN CAST(0 AS DECIMAL(18, 2))
        ELSE
            CAST
            (
                SUM(q.PassQty)
                * AVG(NULLIF(t.SMV, 0))
                * 100.0
                /
                (
                    11
                    * AVG(NULLIF(t.ManPower, 0))
                    * 60
                )
                AS DECIMAL(18, 2)
            )
    END AS ActualEff

FROM QcData q

LEFT JOIN WholePassData w
    ON w.WorkOrderId = q.WorkOrderId
   AND w.UnitId = q.UnitId
   AND w.ProcessModuleId = q.ProcessModuleId
   AND w.WashProcessId = q.WashProcessId

LEFT JOIN IssueData i
    ON i.WorkOrderId = q.WorkOrderId
   AND i.UnitId = q.UnitId
   AND i.ProcessModuleId = q.ProcessModuleId
   AND i.WashProcessId = q.WashProcessId
   AND i.OperationalDate = q.OperationalDate
   AND i.Shift = q.Shift

LEFT JOIN TargetData t
    ON t.UnitId = q.UnitId
   AND t.ProcessModuleId = q.ProcessModuleId
   AND t.WashProcessId = q.WashProcessId
   AND t.OperationalDate = q.OperationalDate
   AND t.Shift = q.Shift

GROUP BY
    q.ProcessModuleId,
    q.ProcessModuleName,

    q.StyleName,
    q.FastReactNo,
    q.WorkOrderNo,

    q.WashProcessId,
    q.ProcessName

ORDER BY
    q.ProcessModuleName,
    q.StyleName,
    q.FastReactNo,
    q.WorkOrderNo,
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

        public const string GetDryProcessHourlyDetails = @"
;WITH PlantFilter AS
(
    SELECT TRY_CAST(LTRIM(RTRIM(value)) AS INT) AS Id
    FROM STRING_SPLIT(@PlantIds, ',')
    WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL
),

UnitFilter AS
(
    SELECT TRY_CAST(LTRIM(RTRIM(value)) AS INT) AS Id
    FROM STRING_SPLIT(@UnitIds, ',')
    WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL
),

ProcessModuleFilter AS
(
    SELECT TRY_CAST(LTRIM(RTRIM(value)) AS INT) AS Id
    FROM STRING_SPLIT(@ProcessModuleIds, ',')
    WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL
),

WashProcessFilter AS
(
    SELECT TRY_CAST(LTRIM(RTRIM(value)) AS INT) AS Id
    FROM STRING_SPLIT(@WashProcessIds, ',')
    WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL
),

ShiftFilter AS
(
    SELECT TRY_CAST(LTRIM(RTRIM(value)) AS INT) AS Id
    FROM STRING_SPLIT(@ShiftList, ',')
    WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL
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
            ELSE
                CAST(DATEADD(DAY, -1, fdpq.CreateDate) AS DATE)
        END AS OperationalDate,

        hs.HourSlot,

        CASE
            WHEN hs.HourSlot BETWEEN 1 AND 12 THEN 1
            ELSE 2
        END AS Shift

    FROM FirstDryProcessQc fdpq

    LEFT JOIN WorkOrder wo
        ON wo.Id = fdpq.WorkOrderId

    CROSS APPLY
    (
        SELECT
            (
                (
                    DATEPART(HOUR, fdpq.CreateDate)
                    - 8
                    + 24
                ) % 24
            ) + 1 AS HourSlot
    ) hs

    WHERE
        fdpq.IsDeleted = 0
        AND fdpq.IsActive = 1

        -- Operational date starts at 08:00 AM
        AND
        (
            @FromDate IS NULL
            OR fdpq.CreateDate >= DATEADD(
                HOUR,
                8,
                CAST(@FromDate AS DATETIME)
            )
        )

        -- Includes up to next day before 08:00 AM
        AND
        (
            @ToDate IS NULL
            OR fdpq.CreateDate < DATEADD(
                HOUR,
                8,
                DATEADD(
                    DAY,
                    1,
                    CAST(@ToDate AS DATETIME)
                )
            )
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
        b.HourSlot,

        SUM(
            CASE
                WHEN b.QcStatusId IN (1, 3) THEN 1
                ELSE 0
            END
        ) AS PassQty,

        SUM(
            CASE
                WHEN b.QcStatusId = 2 THEN 1
                ELSE 0
            END
        ) AS DefectQty,

        SUM(
            CASE
                WHEN b.QcStatusId = 4 THEN 1
                ELSE 0
            END
        ) AS RejectQty

    FROM BaseQc b

    INNER JOIN FirstDryProcess fdp
        ON fdp.Id = b.FirstDryProcessId

    INNER JOIN WashProcess wp
        ON wp.Id = b.WashProcessId

    INNER JOIN ProcessModule pm
        ON pm.Id = fdp.ProcessModuleId

    INNER JOIN PlantUnit pu
        ON pu.Id = fdp.UnitId

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
        b.Shift,
        b.HourSlot
),

IssueData AS
(
    SELECT


        fdp.UnitId,
        fdp.ProcessModuleId,

        b.WashProcessId,
        b.OperationalDate,
        b.Shift,
        b.HourSlot,

        COUNT_BIG(*) AS IssueQty

    FROM FirstDryProcessQcIsuee qi

    INNER JOIN BaseQc b
        ON b.Id = qi.FirstDryProcessQcId

    INNER JOIN FirstDryProcess fdp
        ON fdp.Id = b.FirstDryProcessId

    INNER JOIN PlantUnit pu
        ON pu.Id = fdp.UnitId

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
        b.Shift,
        b.HourSlot
),

TargetData AS
(
    SELECT
        wh.WorkingHourDay AS OperationalDate,
        wh.UnitId,
        wp.ProcessModuleId,
        whdp.WashProcessId,

        hs.HourSlot,

        CASE
            WHEN hs.HourSlot BETWEEN 1 AND 12 THEN 1
            ELSE 2
        END AS Shift,

        SUM(whdp.DailyTarget) AS DayTarget,

        ROUND(
            AVG(CAST(whdp.ManPower AS DECIMAL(18, 2))),
            0
        ) AS ManPower,

        AVG(
            CAST(whdp.SMV AS DECIMAL(18, 2))
        ) AS SMV

    FROM WorkingHourDetailManPower whdp

    INNER JOIN WorkingHourDetail whd
        ON whd.Id = whdp.WorkingHourDetailId

    INNER JOIN WorkingHour wh
        ON wh.Id = whd.WorkingHourId

    INNER JOIN WashProcess wp
        ON wp.Id = whdp.WashProcessId

    INNER JOIN PlantUnit pu
        ON pu.Id = wh.UnitId

    CROSS APPLY
    (
        SELECT
            (
                (
                    DATEPART(HOUR, whd.StartTime)
                    - 8
                    + 24
                ) % 24
            ) + 1 AS HourSlot
    ) hs

    WHERE
        whdp.IsActive = 1
        AND whdp.IsDeleted = 0

        AND
        (
            @FromDate IS NULL
            OR wh.WorkingHourDay >= @FromDate
        )

        AND
        (
            @ToDate IS NULL
            OR wh.WorkingHourDay <= @ToDate
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

        AND
        (
            @ShiftList IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM ShiftFilter sf
                WHERE sf.Id =
                    CASE
                        WHEN hs.HourSlot BETWEEN 1 AND 12 THEN 1
                        ELSE 2
                    END
            )
        )

    GROUP BY
        wh.WorkingHourDay,
        wh.UnitId,
        wp.ProcessModuleId,
        whdp.WashProcessId,
        hs.HourSlot,

        CASE
            WHEN hs.HourSlot BETWEEN 1 AND 12 THEN 1
            ELSE 2
        END
)

SELECT
    q.OperationalDate,

    

    q.HourSlot,

    CONCAT(
        RIGHT(
            '0' + CAST(((q.HourSlot + 7) % 24) AS VARCHAR(2)),
            2
        ),
        ':00 - ',
        RIGHT(
            '0' + CAST(((q.HourSlot + 8) % 24) AS VARCHAR(2)),
            2
        ),
        ':00'
    ) AS HourRange,

    q.ProcessModuleId,
    q.ProcessModuleName,


    q.WashProcessId,
    q.ProcessName,

    SUM(q.PassQty) AS PassQty,
    SUM(q.DefectQty) AS DefectQty,
    SUM(q.RejectQty) AS RejectQty,

    SUM(ISNULL(i.IssueQty, 0)) AS IssueQty,

    SUM(ISNULL(t.DayTarget, 0)) AS DayTarget,

    ROUND(
        AVG(ISNULL(t.ManPower, 0)),
        0
    ) AS ManPower,

    AVG(ISNULL(t.SMV, 0)) AS SMV,

    CASE
        WHEN SUM(q.PassQty) = 0 THEN 0
        ELSE CAST(
            SUM(ISNULL(i.IssueQty, 0))
            * 100.0
            / NULLIF(SUM(q.PassQty), 0)
            AS DECIMAL(18, 2)
        )
    END AS DHU,

    CASE
        WHEN AVG(ISNULL(t.ManPower, 0)) = 0
          OR AVG(ISNULL(t.SMV, 0)) = 0
        THEN 0
        ELSE CAST(
            SUM(ISNULL(t.DayTarget, 0))
            * AVG(ISNULL(t.SMV, 0))
            * 100.0
            /
            (
                1
                * AVG(ISNULL(t.ManPower, 0))
                * 60
            )
            AS DECIMAL(18, 2)
        )
    END AS PlanEff,

    CASE
        WHEN AVG(ISNULL(t.ManPower, 0)) = 0
          OR AVG(ISNULL(t.SMV, 0)) = 0
        THEN 0
        ELSE CAST(
            SUM(q.PassQty)
            * AVG(ISNULL(t.SMV, 0))
            * 100.0
            /
            (
                1
                * AVG(ISNULL(t.ManPower, 0))
                * 60
            )
            AS DECIMAL(18, 2)
        )
    END AS ActualEff

FROM QcData q

LEFT JOIN IssueData i
   on i.UnitId = q.UnitId
   AND i.ProcessModuleId = q.ProcessModuleId
   AND i.WashProcessId = q.WashProcessId
   AND i.OperationalDate = q.OperationalDate
   AND i.Shift = q.Shift
   AND i.HourSlot = q.HourSlot

LEFT JOIN TargetData t
    ON t.UnitId = q.UnitId
   AND t.ProcessModuleId = q.ProcessModuleId
   AND t.WashProcessId = q.WashProcessId
   AND t.OperationalDate = q.OperationalDate
   AND t.Shift = q.Shift
   AND t.HourSlot = q.HourSlot

GROUP BY
    q.OperationalDate,
    q.HourSlot,

    

    q.ProcessModuleId,
    q.ProcessModuleName,


    q.WashProcessId,
    q.ProcessName

ORDER BY
    q.OperationalDate,
    q.HourSlot,
    q.ProcessModuleName,
    q.ProcessName

OPTION (RECOMPILE);
";

        public const string GetWetProcessHourlyDetails = @"
;WITH PlantFilter AS
(
    SELECT TRY_CAST(LTRIM(RTRIM(value)) AS INT) AS Id
    FROM STRING_SPLIT(@PlantIds, ',')
    WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL
),

UnitFilter AS
(
    SELECT TRY_CAST(LTRIM(RTRIM(value)) AS INT) AS Id
    FROM STRING_SPLIT(@UnitIds, ',')
    WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL
),

ProcessModuleFilter AS
(
    SELECT TRY_CAST(LTRIM(RTRIM(value)) AS INT) AS Id
    FROM STRING_SPLIT(@ProcessModuleIds, ',')
    WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL
),

WashProcessFilter AS
(
    SELECT TRY_CAST(LTRIM(RTRIM(value)) AS INT) AS Id
    FROM STRING_SPLIT(@WashProcessIds, ',')
    WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL
),

ShiftFilter AS
(
    SELECT TRY_CAST(LTRIM(RTRIM(value)) AS INT) AS Id
    FROM STRING_SPLIT(@ShiftList, ',')
    WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL
),

BaseQc AS
(
    SELECT
        fdpq.Id,
        fdpq.WashBatchCardId,
        fdpq.WashProcessId,
		fdpq.Quantity,
        fdpq.QcStatusId,
        fdpq.CreateDate,

        CASE
            WHEN CAST(fdpq.CreateDate AS TIME) >= '08:00:00'
                THEN CAST(fdpq.CreateDate AS DATE)
            ELSE
                CAST(DATEADD(DAY, -1, fdpq.CreateDate) AS DATE)
        END AS OperationalDate,

        hs.HourSlot,

        CASE
            WHEN hs.HourSlot BETWEEN 1 AND 12 THEN 1
            ELSE 2
        END AS Shift

    FROM WashBatchCardQc fdpq

    LEFT JOIN WorkOrder wo
        ON wo.Id = fdpq.WorkOrderId

    CROSS APPLY
    (
        SELECT
            (
                (
                    DATEPART(HOUR, fdpq.CreateDate)
                    - 8
                    + 24
                ) % 24
            ) + 1 AS HourSlot
    ) hs

    WHERE
        fdpq.IsDeleted = 0
        AND fdpq.IsActive = 1

        -- Operational date starts at 08:00 AM
        AND
        (
            @FromDate IS NULL
            OR fdpq.CreateDate >= DATEADD(
                HOUR,
                8,
                CAST(@FromDate AS DATETIME)
            )
        )

        -- Includes up to next day before 08:00 AM
        AND
        (
            @ToDate IS NULL
            OR fdpq.CreateDate < DATEADD(
                HOUR,
                8,
                DATEADD(
                    DAY,
                    1,
                    CAST(@ToDate AS DATETIME)
                )
            )
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
        b.HourSlot,
		SUM(CASE WHEN b.QcStatusId IN (1,3) THEN b.Quantity ELSE 0 END) AS PassQty,
		 SUM(CASE WHEN b.QcStatusId = 2 THEN b.Quantity ELSE 0 END) AS DefectQty,
        SUM(CASE WHEN b.QcStatusId = 4 THEN b.Quantity ELSE 0 END) AS RejectQty
        
    FROM BaseQc b

    INNER JOIN WashBatchCard fdp
        ON fdp.Id = b.WashBatchCardId

    INNER JOIN WashProcess wp
        ON wp.Id = b.WashProcessId

    INNER JOIN ProcessModule pm
        ON pm.Id = fdp.ProcessModuleId

    INNER JOIN PlantUnit pu
        ON pu.Id = fdp.UnitId

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
        b.Shift,
        b.HourSlot
),

IssueData AS
(
    SELECT


        fdp.UnitId,
        fdp.ProcessModuleId,

        b.WashProcessId,
        b.OperationalDate,
        b.Shift,
        b.HourSlot,

        COUNT_BIG(*) AS IssueQty

    FROM WashBatchCardQcIsue qi

    INNER JOIN BaseQc b
        ON b.Id = qi.WashBatchCardQcId

    INNER JOIN WashBatchCard fdp
        ON fdp.Id = b.WashBatchCardId

    INNER JOIN PlantUnit pu
        ON pu.Id = fdp.UnitId

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
        b.Shift,
        b.HourSlot
),

TargetData AS
(
    SELECT
        wh.WorkingHourDay AS OperationalDate,
        wh.UnitId,
        wp.ProcessModuleId,
        whdp.WashProcessId,

        hs.HourSlot,

        CASE
            WHEN hs.HourSlot BETWEEN 1 AND 12 THEN 1
            ELSE 2
        END AS Shift,

        SUM(whdp.DailyTarget) AS DayTarget,

        ROUND(
            AVG(CAST(whdp.ManPower AS DECIMAL(18, 2))),
            0
        ) AS ManPower,

        AVG(
            CAST(whdp.SMV AS DECIMAL(18, 2))
        ) AS SMV

    FROM WorkingHourDetailManPower whdp

    INNER JOIN WorkingHourDetail whd
        ON whd.Id = whdp.WorkingHourDetailId

    INNER JOIN WorkingHour wh
        ON wh.Id = whd.WorkingHourId

    INNER JOIN WashProcess wp
        ON wp.Id = whdp.WashProcessId

    INNER JOIN PlantUnit pu
        ON pu.Id = wh.UnitId

    CROSS APPLY
    (
        SELECT
            (
                (
                    DATEPART(HOUR, whd.StartTime)
                    - 8
                    + 24
                ) % 24
            ) + 1 AS HourSlot
    ) hs

    WHERE
        whdp.IsActive = 1
        AND whdp.IsDeleted = 0

        AND
        (
            @FromDate IS NULL
            OR wh.WorkingHourDay >= @FromDate
        )

        AND
        (
            @ToDate IS NULL
            OR wh.WorkingHourDay <= @ToDate
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

        AND
        (
            @ShiftList IS NULL
            OR EXISTS
            (
                SELECT 1
                FROM ShiftFilter sf
                WHERE sf.Id =
                    CASE
                        WHEN hs.HourSlot BETWEEN 1 AND 12 THEN 1
                        ELSE 2
                    END
            )
        )

    GROUP BY
        wh.WorkingHourDay,
        wh.UnitId,
        wp.ProcessModuleId,
        whdp.WashProcessId,
        hs.HourSlot,

        CASE
            WHEN hs.HourSlot BETWEEN 1 AND 12 THEN 1
            ELSE 2
        END
)

SELECT
    q.OperationalDate,

    

    q.HourSlot,

    CONCAT(
        RIGHT(
            '0' + CAST(((q.HourSlot + 7) % 24) AS VARCHAR(2)),
            2
        ),
        ':00 - ',
        RIGHT(
            '0' + CAST(((q.HourSlot + 8) % 24) AS VARCHAR(2)),
            2
        ),
        ':00'
    ) AS HourRange,

    q.ProcessModuleId,
    q.ProcessModuleName,


    q.WashProcessId,
    q.ProcessName,

    SUM(q.PassQty) AS PassQty,
    SUM(q.DefectQty) AS DefectQty,
    SUM(q.RejectQty) AS RejectQty,

    SUM(ISNULL(i.IssueQty, 0)) AS IssueQty,

    SUM(ISNULL(t.DayTarget, 0)) AS DayTarget,

    ROUND(
        AVG(ISNULL(t.ManPower, 0)),
        0
    ) AS ManPower,

    AVG(ISNULL(t.SMV, 0)) AS SMV,

    CASE
        WHEN SUM(q.PassQty) = 0 THEN 0
        ELSE CAST(
            SUM(ISNULL(i.IssueQty, 0))
            * 100.0
            / NULLIF(SUM(q.PassQty), 0)
            AS DECIMAL(18, 2)
        )
    END AS DHU,

    CASE
        WHEN AVG(ISNULL(t.ManPower, 0)) = 0
          OR AVG(ISNULL(t.SMV, 0)) = 0
        THEN 0
        ELSE CAST(
            SUM(ISNULL(t.DayTarget, 0))
            * AVG(ISNULL(t.SMV, 0))
            * 100.0
            /
            (
                1
                * AVG(ISNULL(t.ManPower, 0))
                * 60
            )
            AS DECIMAL(18, 2)
        )
    END AS PlanEff,

    CASE
        WHEN AVG(ISNULL(t.ManPower, 0)) = 0
          OR AVG(ISNULL(t.SMV, 0)) = 0
        THEN 0
        ELSE CAST(
            SUM(q.PassQty)
            * AVG(ISNULL(t.SMV, 0))
            * 100.0
            /
            (
                1
                * AVG(ISNULL(t.ManPower, 0))
                * 60
            )
            AS DECIMAL(18, 2)
        )
    END AS ActualEff

FROM QcData q

LEFT JOIN IssueData i
   on i.UnitId = q.UnitId
   AND i.ProcessModuleId = q.ProcessModuleId
   AND i.WashProcessId = q.WashProcessId
   AND i.OperationalDate = q.OperationalDate
   AND i.Shift = q.Shift
   AND i.HourSlot = q.HourSlot

LEFT JOIN TargetData t
    ON t.UnitId = q.UnitId
   AND t.ProcessModuleId = q.ProcessModuleId
   AND t.WashProcessId = q.WashProcessId
   AND t.OperationalDate = q.OperationalDate
   AND t.Shift = q.Shift
   AND t.HourSlot = q.HourSlot

GROUP BY
    q.OperationalDate,
    q.Shift,
    q.HourSlot,

    

    q.ProcessModuleId,
    q.ProcessModuleName,


    q.WashProcessId,
    q.ProcessName

ORDER BY
    q.OperationalDate,
    q.HourSlot,
    q.ProcessModuleName,
    q.ProcessName

OPTION (RECOMPILE);
";
    }
}
