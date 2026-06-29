namespace wsahRecieveDelivary.Queries
{
    public static class TusukaExtremeQuery
    {
        public const string GetWashDelivery = @"
SELECT 
    SUM(CASE 
            WHEN wop.ProcessId = 315 
            THEN ISNULL(wop.Quantity, 0) 
            ELSE 0 
        END) AS Receive,

    SUM(CASE 
            WHEN wop.ProcessId = 316 
            THEN ISNULL(wop.Quantity, 0) 
            ELSE 0 
        END) AS Delivery

FROM [TusukaExtreme].[dbo].[MA_WorkOrderProduction] wop
JOIN [TusukaExtreme].[dbo].[MA_WorkOrderItem] woi
    ON wop.WorkOrderItemId = woi.RecId
JOIN [TusukaExtreme].[dbo].[MA_Process] p
    ON wop.ProcessId = p.RecId

WHERE wop.ProcessId IN (315, 316)

  AND (@FromDate IS NULL OR wop.ProductionDate >= @FromDate)
  AND (@ToDate IS NULL OR wop.ProductionDate < DATEADD(DAY, 1, @ToDate))

  -- Plant Filter
  AND
  (
        @PlantCount = 0

        OR 
        (
            'TPL' IN @Plant
            AND wop.UD_WashUnit IN 
            (
                'Unit 1', 
                'Unit 2', 
                'Unit 3', 
                'Unit 4', 
                'Unit 5'
            )
        )

        OR 
        (
            'TWL' IN @Plant
            AND wop.UD_WashUnit = 'Unit TWL'
        )
  )

  -- Wash Unit Filter
  AND 
  (
        @WashUnitCount = 0
        OR wop.UD_WashUnit IN @WashUnit
  );
";



        public const string GetWashDeliveryDetails = @"
;WITH AllProductionAgg AS
(
    SELECT
        WorkOrderItemId,

        SUM(CASE 
                WHEN ProcessId = 315 
                THEN ISNULL(Quantity, 0) 
                ELSE 0 
            END) AS TotalReceived,

        SUM(CASE 
                WHEN ProcessId = 316 
                THEN ISNULL(Quantity, 0) 
                ELSE 0 
            END) AS TotalSend,

        MAX(CASE 
                WHEN ISNULL(Explanation, '') <> '' 
                 AND ProcessId = 315 
                THEN Explanation 
            END) AS Marks
    FROM MA_WorkOrderProduction WITH (NOLOCK)
    GROUP BY WorkOrderItemId
),

DateProductionAgg AS
(
    SELECT
        WorkOrderItemId,

        CAST(MAX(ProductionDate) AS DATE) AS ProductionDate,

        SUM(CASE 
                WHEN ProcessId = 315 
                THEN ISNULL(Quantity, 0) 
                ELSE 0 
            END) AS Receive,

        SUM(CASE 
                WHEN ProcessId = 316 
                THEN ISNULL(Quantity, 0) 
                ELSE 0 
            END) AS Delivery

    FROM MA_WorkOrderProduction WITH (NOLOCK)
    WHERE ProcessId IN (315, 316)
      AND (@FromDate IS NULL OR ProductionDate >= @FromDate)
      AND (@ToDate IS NULL OR ProductionDate < DATEADD(DAY, 1, @ToDate))
    GROUP BY WorkOrderItemId
),

WOP AS
(
    SELECT
        allAgg.WorkOrderItemId,
        dateAgg.ProductionDate,

        ISNULL(allAgg.TotalReceived, 0) AS TotalReceived,
        ISNULL(allAgg.TotalSend, 0) AS TotalSend,

        ISNULL(dateAgg.Receive, 0) AS Receive,
        ISNULL(dateAgg.Delivery, 0) AS Delivery,

        allAgg.Marks
    FROM AllProductionAgg allAgg
    INNER JOIN DateProductionAgg dateAgg
        ON dateAgg.WorkOrderItemId = allAgg.WorkOrderItemId
),

BaseData AS
(
    SELECT
        WOP.ProductionDate,

        ISNULL(FWF.Factory, '') AS Factory,
        ISNULL(R.ResourceCode, '') AS Unit,

        ISNULL(Ac.CurrentAccountName, '') + '::' +
        ISNULL(ID.DepartmentName, '') AS Buyer,

        ISNULL(X.WorkOrderNo, '') AS WorkOrderNo,
        ISNULL(I.InventoryName, '') AS StyleName,
        ISNULL(WOI.UD_FastReactNo, '') AS FastReactNo,

        (
            SELECT TOP 1 ISNULL(VI.ItemName, '')
            FROM IM_VariantItem VI WITH (NOLOCK)
            WHERE VI.CompanyId = W.CompanyId
              AND VI.ItemCode = WOI.OperationCode
              AND VI.CardId = 1
        ) AS Color,

        ISNULL(WOI.Quantity, 0) AS OrderQuantity,

        X.UD_InitialEndDate AS WashTargetDate,
        WOI.DepartureDate AS TOD,

        ISNULL(WOP.TotalReceived, 0) AS TotalWashReceived,
        ISNULL(WOP.TotalSend, 0) AS TotalWashDelivery,

        ISNULL(WOP.Receive, 0) AS Receive,
        ISNULL(WOP.Delivery, 0) AS Delivery

    FROM MA_WorkOrder W WITH (NOLOCK)

    LEFT JOIN MA_WorkOrderItem WI WITH (NOLOCK)
        ON W.RecId = WI.WorkOrderId
       AND WI.WorkOrderSubType = 1

    LEFT JOIN MA_WorkOrderItem WOI WITH (NOLOCK)
        ON WOI.WorkOrderId = W.RecId
       AND WOI.WorkOrderSubType = 2
       AND WOI.ParentItemId IS NULL

    LEFT JOIN IM_Item I WITH (NOLOCK)
        ON WI.InventoryId = I.RecId

    LEFT JOIN FI_Account Ac WITH (NOLOCK)
        ON Ac.RecId = W.CurrentAccountId

    LEFT JOIN IM_ItemDepartment ID WITH (NOLOCK)
        ON I.ItemDepartmentId = ID.RecId

    LEFT JOIN TSK_WashWorkOrderItem TWOI WITH (NOLOCK)
        ON TWOI.DocketWorkOrderItemId = WOI.RecId

    LEFT JOIN MA_WorkOrderItem MWI WITH (NOLOCK)
        ON MWI.RecId = TWOI.WashWorkOrderItemId

    LEFT JOIN MA_WorkOrder X WITH (NOLOCK)
        ON X.RecId = MWI.WorkOrderId

    LEFT JOIN MA_Resource R WITH (NOLOCK)
        ON R.RecId = X.ResourceId

    LEFT JOIN TSK_FastReactWashFile FWF WITH (NOLOCK)
        ON FWF.RecId =
        (
            SELECT MAX(TSK.RecId)
            FROM TSK_FastReactWashFile TSK WITH (NOLOCK, INDEX = TSK_FastReactWashFile_IX1)
            WHERE TSK.OrderCode = WOI.UD_FastReactNo
        )

    INNER JOIN WOP
        ON WOP.WorkOrderItemId = MWI.RecId

    WHERE
        W.WorkOrderType = 15
        AND X.Status <> 5
        AND W.Status = 102
        AND ISNULL(W.IsPLM, 0) = 0
        AND ISNULL(W.IsClosed, 0) = 0
        AND ISNULL(W.IsVirtual, 0) = 0
        AND DATEADD(MONTH, 20, W.WorkOrderDate) > GETDATE()

        /* Show work orders that have Receive or Delivery in selected date */
        AND 
        (
            ISNULL(WOP.Receive, 0) > 0
            OR ISNULL(WOP.Delivery, 0) > 0
        )

        /* Plant Filter */
        AND
        (
            @PlantCount = 0

            OR
            (
                'TPL' IN @Plant
                AND R.ResourceCode IN ('Unit 1', 'Unit 2', 'Unit 3', 'Unit 4', 'Unit 5')
            )

            OR
            (
                'TWL' IN @Plant
                AND R.ResourceCode = 'Unit TWL'
            )
        )

        /* Unit Filter */
        AND
        (
            @WashUnitCount = 0
            OR R.ResourceCode IN @WashUnit
        )
),

FinalData AS
(
    SELECT
        ProductionDate,
        Factory,
        Unit,
        Buyer,
        WorkOrderNo,
        StyleName,
        FastReactNo,
        Color,

        SUM(ISNULL(OrderQuantity, 0)) AS OrderQuantity,

        WashTargetDate,
        TOD,

        SUM(ISNULL(TotalWashReceived, 0)) AS TotalWashReceived,
        SUM(ISNULL(TotalWashDelivery, 0)) AS TotalWashDelivery,

        SUM(ISNULL(Receive, 0)) AS Receive,
        SUM(ISNULL(Delivery, 0)) AS Delivery

    FROM BaseData
    GROUP BY
        ProductionDate,
        Factory,
        Unit,
        Buyer,
        WorkOrderNo,
        StyleName,
        FastReactNo,
        Color,
        WashTargetDate,
        TOD
),

CountData AS
(
    SELECT COUNT(*) AS TotalRecords
    FROM FinalData
)

SELECT
    fd.*,
    cd.TotalRecords
FROM FinalData fd
CROSS JOIN CountData cd
ORDER BY
    fd.ProductionDate DESC,
    fd.Factory,
    fd.Unit,
    fd.WorkOrderNo
OFFSET @Offset ROWS
FETCH NEXT @PageSize ROWS ONLY;
";

    }
}
