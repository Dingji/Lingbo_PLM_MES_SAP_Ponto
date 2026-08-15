/*     */ package com.jiexin.plm.px;
/*     */ import com.agile.api.APIException;
/*     */ import com.agile.api.IAgileSession;
/*     */ import com.agile.api.IChange;
/*     */ import com.agile.api.IDataObject;
/*     */ import com.agile.api.IItem;
/*     */ import com.agile.api.IRow;
/*     */ import com.agile.api.ITable;
/*     */ import com.agile.api.ItemConstants;
/*     */ import com.agile.px.ActionResult;
/*     */ import com.alibaba.fastjson.JSON;
/*     */ import com.jiexin.plm.util.AgileUtil;
/*     */ import java.util.ArrayList;
/*     */ import java.util.HashMap;
/*     */ import java.util.List;
/*     */ import java.util.Map;
/*     */ import java.util.stream.Collectors;
/*     */ 
/*     */ public class ItemToSapCreate implements ICustomAction {
/*  20 */   private static Logger logger = null;
/*     */ 
/*     */   
/*     */   static {
/*     */     try {
/*  25 */       String logXML = LoggerUtil.getLoggerUtil();
/*     */       
/*  27 */       LoggerContext ctx = Configurator.initialize(null, (new File(logXML)).toURI().toString());
/*  28 */       logger = ctx.getLogger(ItemToSapCreate.class.getName());
/*  29 */     } catch (Exception ex) {
/*  30 */       ex.printStackTrace();
/*     */     } 
/*     */   }
/*     */ 
/*     */   
/*     */   public ActionResult doAction(IAgileSession iAgileSession, INode iNode, IDataObject iDataObject) {
/*  36 */     IChange change = (IChange)iDataObject;
/*  37 */     List<String> error = new ArrayList<>();
/*  38 */     List<String> success = new ArrayList<>();
/*  39 */     Map<String, List<String>> itemResultMaps = null;
/*     */     try {
/*  41 */       itemResultMaps = addItem(change, iAgileSession);
/*  42 */     } catch (APIException e) {
/*  43 */       throw new RuntimeException(e);
/*     */     } 
/*  45 */     List<String> createSuccess = itemResultMaps.get("S");
/*  46 */     List<String> createError = itemResultMaps.get("E");
/*  47 */     if (createSuccess != null && createSuccess.size() > 0) {
/*  48 */       logger.info("当前变更单物料抛送SAP成功数据有:{}", createSuccess);
/*  49 */       success.add("物料抛送成功:" + (String)createSuccess.stream().collect(Collectors.joining(",")));
/*     */     } 
/*     */     
/*  52 */     if (createError != null && createError.size() > 0) {
/*  53 */       logger.error("物料抛送SAP失败数据有:{}", createSuccess);
/*  54 */       String reason = createError.stream().collect(Collectors.joining(","));
/*  55 */       error.add("物料抛送失败:" + reason);
/*     */     } 
/*  57 */     if (error.size() <= 0) {
/*  58 */       return new ActionResult(0, "SAP创建成功！！！");
/*     */     }
/*  60 */     throw new RuntimeException((String)error.stream().collect(Collectors.joining(" | ")));
/*     */   }
/*     */ 
/*     */ 
/*     */   
/*     */   public static Map<String, List<String>> addItem(IChange change, IAgileSession session) throws APIException {
/*  66 */     Map<String, List<String>> map = new HashMap<>();
/*  67 */     ITable table = change.getTable(ChangeConstants.TABLE_AFFECTEDITEMS);
/*  68 */     int size = table.size();
/*  69 */     if (size <= 0) {
/*  70 */       logger.error("当前变更单没有受影响物件");
/*     */     }
/*  72 */     List<Map<String, Object>> itemAddOrUpdateList = new ArrayList<>();
/*  73 */     for (Object o : table) {
/*  74 */       IRow row = (IRow)o;
/*  75 */       IItem item = (IItem)row.getReferent();
/*  76 */       String isMdm = item.getCell(ItemConstants.ATT_PAGE_TWO_LIST01).getValue().toString();
/*  77 */       String apiName = item.getAgileClass().getSuperClass().getAPIName();
/*  78 */       if (apiName.equals("PartsClass") && !isMdm.equals("Y")) {
/*  79 */         ItemToSapTestPx.addSapItem(item, row, itemAddOrUpdateList, "01");
/*     */       }
/*     */     } 
/*     */ 
/*     */     
/*  84 */     logger.info("当前新增或修改物料:" + JSON.toJSON(itemAddOrUpdateList));
/*  85 */     System.out.println("itemAddOrUpdateList = " + JSON.toJSON(itemAddOrUpdateList));
/*  86 */     if (itemAddOrUpdateList.size() > 0) {
/*     */       
/*     */       try {
/*  89 */         logger.info(">>>>>>>>>>>正在向SAP抛送数据<<<<<<<<<<<<");
/*  90 */         map = PlmRequestSap.plmToSapItem(itemAddOrUpdateList);
/*  91 */         List<String> success = map.get("S");
/*  92 */         logger.info("成功物料:{}", success);
/*     */         
/*  94 */         List<String> error = map.get("E");
/*  95 */         logger.info("失败物料:{}", error);
/*     */       }
/*  97 */       catch (Exception e) {
/*  98 */         e.printStackTrace();
/*  99 */         throw new RuntimeException(e.getMessage(), e);
/*     */       } 
/*     */     }
/*     */     
/* 103 */     return map;
/*     */   }
/*     */ 
/*     */   
/*     */   private static void addSapItem(IItem item, IRow row, List<Map<String, Object>> itemAddList, String addOrUpdate) {
/*     */     try {
/* 109 */       String value = AgileUtil.getValue((IDataObject)item, ItemConstants.ATT_PAGE_TWO_MULTILIST02);
/* 110 */       String[] split = value.split(";");
/* 111 */       String werks = AgileUtil.getRedlineValueByItem(item, Integer.valueOf(2090));
/* 112 */       String[] werkList = werks.split(";");
/* 113 */       for (String werk : werkList) {
/* 114 */         Map<String, Object> sapItem = new HashMap<>();
/*     */ 
/*     */ 
/*     */ 
/*     */         
/* 119 */         sapItem.put("ZCZLX", addOrUpdate);
/*     */ 
/*     */ 
/*     */         
/* 123 */         String desc = AgileUtil.getValue((IDataObject)item, Integer.valueOf(1002));
/* 124 */         sapItem.put("ZCWB", desc);
/*     */ 
/*     */         
/* 127 */         String itemType = AgileUtil.getRedlineListApi(item, Integer.valueOf(1274));
/* 128 */         sapItem.put("MTART", itemType);
/*     */ 
/*     */ 
/*     */         
/* 132 */         String itemNumber = item.getName();
/*     */ 
/*     */ 
/*     */         
/* 136 */         sapItem.put("BISMT", itemNumber);
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */         
/* 147 */         String lev = AgileUtil.getRedlineListApi(item, Integer.valueOf(1275));
/* 148 */         sapItem.put("PRDHA", lev);
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */         
/* 154 */         String name = item.getAgileClass().getName();
/* 155 */         String matkl = ItemToMdmTestPx.subFirstN(name, 5);
/*     */         
/* 157 */         sapItem.put("MATKL", matkl);
/*     */ 
/*     */         
/* 160 */         String mein = AgileUtil.getValue((IDataObject)item, Integer.valueOf(1271));
/*     */         
/* 162 */         sapItem.put("MEINS", mein);
/*     */ 
/*     */ 
/*     */         
/* 166 */         String lifeType = row.getCell(Integer.valueOf(1057)).getValue().toString();
/*     */ 
/*     */ 
/*     */ 
/*     */         
/* 171 */         if (lifeType.equals("停用") || lifeType.equals("EOL")) {
/* 172 */           sapItem.put("ZZT", "ZTT");
/*     */         } else {
/* 174 */           sapItem.put("ZZT", "ZSC");
/*     */         } 
/*     */ 
/*     */         
/* 178 */         logger.info("werks{}", werk);
/* 179 */         sapItem.put("WERKS", werk);
/*     */ 
/*     */         
/* 182 */         if (lifeType.equals("EV") || lifeType.equals("DV") || lifeType.equals("PV")) {
/* 183 */           sapItem.put("STAGE", "A");
/* 184 */         } else if (lifeType.equals("MP")) {
/* 185 */           sapItem.put("STAGE", "B");
/*     */         } 
/*     */         
/* 188 */         sapItem.put("ZKPDW", "个");
/* 189 */         sapItem.put("ZZZPL", "物料组");
/*     */ 
/*     */         
/* 192 */         itemAddList.add(sapItem);
/*     */       }
/*     */     
/* 195 */     } catch (Exception e) {
/* 196 */       e.printStackTrace();
/* 197 */       throw new RuntimeException("获取属性异常", e);
/*     */     } 
/*     */   }
/*     */ }


/* Location:              {{SOURCE_PATH}}\com\jiexin\plm\px\ItemToSapCreate.class
 * Java compiler version: 8 (52.0)
 * JD-Core Version:       1.1.3
 */