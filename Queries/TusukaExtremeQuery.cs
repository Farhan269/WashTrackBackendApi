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
    /* =========================================================
       ALL TIME PRODUCTION
       One row per WorkOrderItemId
       ========================================================= */
    SELECT
        WOP.WorkOrderItemId,

        SUM(
            CASE
                WHEN WOP.ProcessId = 315
                THEN ISNULL(WOP.Quantity, 0)
                ELSE 0
            END
        ) AS TotalReceived,

        SUM(
            CASE
                WHEN WOP.ProcessId = 316
                THEN ISNULL(WOP.Quantity, 0)
                ELSE 0
            END
        ) AS TotalSend,

        MAX(
            CASE
                WHEN ISNULL(WOP.Explanation, '') <> ''
                     AND WOP.ProcessId = 315
                THEN WOP.Explanation
            END
        ) AS Marks

    FROM MA_WorkOrderProduction WOP

    WHERE WOP.ProcessId IN (315, 316)

    GROUP BY
        WOP.WorkOrderItemId
),


DateProductionAgg AS
(
    /* =========================================================
       SELECTED DATE PRODUCTION

       IMPORTANT:
       Plant + Unit filtering happens DIRECTLY on
       MA_WorkOrderProduction.UD_WashUnit.

       This is the same source/logic as your correct summary API.
       ========================================================= */
    SELECT
        WOP.WorkOrderItemId,

        WOP.UD_WashUnit AS Unit,

        CAST(MAX(WOP.ProductionDate) AS DATE) AS ProductionDate,

        SUM(
            CASE
                WHEN WOP.ProcessId = 315
                THEN ISNULL(WOP.Quantity, 0)
                ELSE 0
            END
        ) AS Receive,

        SUM(
            CASE
                WHEN WOP.ProcessId = 316
                THEN ISNULL(WOP.Quantity, 0)
                ELSE 0
            END
        ) AS Delivery

    FROM MA_WorkOrderProduction WOP

    INNER JOIN MA_WorkOrderItem WOI
        ON WOP.WorkOrderItemId = WOI.RecId

    INNER JOIN MA_Process P
        ON WOP.ProcessId = P.RecId

    WHERE
        WOP.ProcessId IN (315, 316)

        AND
        (
            @FromDate IS NULL
            OR WOP.ProductionDate >= @FromDate
        )

        AND
        (
            @ToDate IS NULL
            OR WOP.ProductionDate < DATEADD(DAY, 1, @ToDate)
        )

        /* =====================================================
           PLANT FILTER
           IMPORTANT: use WOP.UD_WashUnit
           Same as summary query
           ===================================================== */
        AND
        (
            @PlantCount = 0

            OR
            (
                'TPL' IN @Plant

                AND WOP.UD_WashUnit IN
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
                AND WOP.UD_WashUnit = 'Unit TWL'
            )
        )

        /* =====================================================
           WASH UNIT FILTER
           IMPORTANT: use WOP.UD_WashUnit
           ===================================================== */
        AND
        (
            @WashUnitCount = 0
            OR WOP.UD_WashUnit IN @WashUnit
        )

    GROUP BY
        WOP.WorkOrderItemId,
        WOP.UD_WashUnit
),


WOP AS
(
    /* =========================================================
       MERGE SELECTED DATE + ALL TIME PRODUCTION
       Still one row per WorkOrderItemId / Unit
       ========================================================= */
    SELECT
        DPA.WorkOrderItemId,

        DPA.ProductionDate,

        DPA.Unit,

        ISNULL(APA.TotalReceived, 0) AS TotalReceived,

        ISNULL(APA.TotalSend, 0) AS TotalSend,

        ISNULL(DPA.Receive, 0) AS Receive,

        ISNULL(DPA.Delivery, 0) AS Delivery,

        APA.Marks

    FROM DateProductionAgg DPA

    LEFT JOIN AllProductionAgg APA
        ON APA.WorkOrderItemId = DPA.WorkOrderItemId
),


ProductionWithWorkOrder AS
(
    /* =========================================================
       Direct relationship:

       MA_WorkOrderProduction.WorkOrderItemId
                    ↓
       MA_WorkOrderItem.RecId
                    ↓
       MA_WorkOrder.WorkOrderNo

       No docket join required for production amount.
       ========================================================= */
    SELECT
        WOP.ProductionDate,

        WOP.WorkOrderItemId,

        WOP.Unit,

        WOP.TotalReceived,

        WOP.TotalSend,

        WOP.Receive,

        WOP.Delivery,

        WOP.Marks,

        MWI.WorkOrderId AS WashWorkOrderId,

        ISNULL(X.WorkOrderNo, '') AS WorkOrderNo,

        X.UD_InitialEndDate AS WashTargetDate

    FROM WOP

    INNER JOIN MA_WorkOrderItem MWI
        ON MWI.RecId = WOP.WorkOrderItemId

    LEFT JOIN MA_WorkOrder X
        ON X.RecId = MWI.WorkOrderId
),


BaseData AS
(
    /* =========================================================
       ATTACH METADATA ONLY

       OUTER APPLY TOP 1 ensures one production row remains
       exactly one production row.

       Therefore Receive / Delivery cannot multiply.
       ========================================================= */
    SELECT
        PWO.ProductionDate,

        ISNULL(MD.Factory, '') AS Factory,

        /* IMPORTANT:
           Unit is directly from MA_WorkOrderProduction
        */
        ISNULL(PWO.Unit, '') AS Unit,

        ISNULL(MD.Buyer, '') AS Buyer,

        ISNULL(PWO.WorkOrderNo, '') AS WorkOrderNo,

        ISNULL(MD.StyleName, '') AS StyleName,

        ISNULL(MD.FastReactNo, '') AS FastReactNo,

        ISNULL(MD.Color, '') AS Color,

        ISNULL(MD.OrderQuantity, 0) AS OrderQuantity,

        PWO.WashTargetDate,

        MD.TOD,

        ISNULL(PWO.TotalReceived, 0) AS TotalWashReceived,

        ISNULL(PWO.TotalSend, 0) AS TotalWashDelivery,

        ISNULL(PWO.Receive, 0) AS Receive,

        ISNULL(PWO.Delivery, 0) AS Delivery,

        PWO.Marks,

        PWO.WorkOrderItemId

    FROM ProductionWithWorkOrder PWO


    OUTER APPLY
    (
        SELECT TOP 1

            ISNULL(FWF.Factory, '') AS Factory,

            ISNULL(AC.CurrentAccountName, '')
            + '::'
            + ISNULL(ID.DepartmentName, '') AS Buyer,

            ISNULL(I.InventoryName, '') AS StyleName,

            ISNULL(DWOI.UD_FastReactNo, '') AS FastReactNo,

            ISNULL(CLR.Color, '') AS Color,

            ISNULL(DWOI.Quantity, 0) AS OrderQuantity,

            DWOI.DepartureDate AS TOD

        FROM TSK_WashWorkOrderItem TWOI


        INNER JOIN MA_WorkOrderItem DWOI
            ON DWOI.RecId = TWOI.DocketWorkOrderItemId


        INNER JOIN MA_WorkOrder DW
            ON DW.RecId = DWOI.WorkOrderId


        /* =====================================================
           GET ONE STYLE ITEM ONLY
           Prevent WI multiplication
           ===================================================== */
        OUTER APPLY
        (
            SELECT TOP 1
                WI.InventoryId

            FROM MA_WorkOrderItem WI

            WHERE
                WI.WorkOrderId = DW.RecId
                AND WI.WorkOrderSubType = 1

            ORDER BY
                WI.RecId
        ) STYLE_ITEM


        LEFT JOIN IM_Item I
            ON I.RecId = STYLE_ITEM.InventoryId


        LEFT JOIN FI_Account AC
            ON AC.RecId = DW.CurrentAccountId


        LEFT JOIN IM_ItemDepartment ID
            ON ID.RecId = I.ItemDepartmentId


        /* =====================================================
           COLOR
           ===================================================== */
        OUTER APPLY
        (
            SELECT TOP 1
                ISNULL(VI.ItemName, '') AS Color

            FROM IM_VariantItem VI

            WHERE
                VI.CompanyId = DW.CompanyId
                AND VI.ItemCode = DWOI.OperationCode
                AND VI.CardId = 1

            ORDER BY
                VI.RecId
        ) CLR


        /* =====================================================
           LATEST FAST REACT WASH FILE
           ===================================================== */
        OUTER APPLY
        (
            SELECT TOP 1
                TSK.Factory

            FROM TSK_FastReactWashFile TSK

            WHERE
                TSK.OrderCode = DWOI.UD_FastReactNo

            ORDER BY
                TSK.RecId DESC
        ) FWF


        WHERE
            TWOI.WashWorkOrderItemId = PWO.WorkOrderItemId

        ORDER BY
            TWOI.RecId DESC

    ) MD


    /* =========================================================
       IMPORTANT

       Do NOT put old W / X status filters here if you want
       Receive/Delivery total to match the summary API exactly.

       Production itself determines which rows are included.
       ========================================================= */

    WHERE
        ISNULL(PWO.Receive, 0) > 0
        OR ISNULL(PWO.Delivery, 0) > 0
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

        /* Metadata value - do not multiply it */
        MAX(ISNULL(OrderQuantity, 0)) AS OrderQuantity,

        WashTargetDate,

        MAX(TOD) AS TOD,

        /* =====================================================
           Lifetime totals

           BaseData has one row per unique production
           WorkOrderItemId / Unit, so summing distinct item
           totals here is safe.
           ===================================================== */
        SUM(ISNULL(TotalWashReceived, 0))
            AS TotalWashReceived,

        SUM(ISNULL(TotalWashDelivery, 0))
            AS TotalWashDelivery,

        /* =====================================================
           Selected period totals
           These should match your summary query.
           ===================================================== */
        SUM(ISNULL(Receive, 0))
            AS Receive,

        SUM(ISNULL(Delivery, 0))
            AS Delivery

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
        WashTargetDate
),


CountData AS
(
    SELECT
        COUNT(*) AS TotalRecords

    FROM FinalData
)


SELECT
    FD.ProductionDate,

    FD.Factory,

    FD.Unit,

    FD.Buyer,

    FD.WorkOrderNo,

    FD.StyleName,

    FD.FastReactNo,

    FD.Color,

    FD.OrderQuantity,

    FD.WashTargetDate,

    FD.TOD,

    FD.TotalWashReceived,

    FD.TotalWashDelivery,

    FD.Receive,

    FD.Delivery,

    CD.TotalRecords

FROM FinalData FD

CROSS JOIN CountData CD

ORDER BY
    FD.ProductionDate DESC,
    FD.Factory,
    FD.Unit,
    FD.WorkOrderNo

OFFSET @Offset ROWS
FETCH NEXT @PageSize ROWS ONLY;
";

    }
}
