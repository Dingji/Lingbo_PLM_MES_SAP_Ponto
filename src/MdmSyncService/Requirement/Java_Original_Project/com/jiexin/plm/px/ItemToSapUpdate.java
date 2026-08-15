/*     */ package com.jiexin.plm.px;
/*     */ import com.agile.api.APIException;
/*     */ import com.agile.api.ChangeConstants;
/*     */ import com.agile.api.IAgileSession;
/*     */ import com.agile.api.IChange;
/*     */ import com.agile.api.IDataObject;
/*     */ import com.agile.api.IItem;
/*     */ import com.agile.api.IRow;
/*     */ import com.agile.api.ITable;
/*     */ import com.agile.api.ItemConstants;
/*     */ import com.agile.px.ActionResult;
/*     */ import com.agile.px.ICustomAction;
/*     */ import com.alibaba.fastjson.JSON;
/*     */ import com.jiexin.plm.util.AgileUtil;
/*     */ import com.jiexin.plm.util.LoggerUtil;
/*     */ import com.jiexin.plm.util.PlmRequestSap;
/*     */ import java.util.ArrayList;
/*     */ import java.util.HashMap;
/*     */ import java.util.List;
/*     */ import java.util.Map;
/*     */ import java.util.stream.Collectors;
/*     */ import org.apache.logging.log4j.core.Logger;
/*     */ import org.apache.logging.log4j.core.LoggerContext;
/*     */ 
/*     */ public class ItemToSapUpdate implements ICustomAction {
/*  26 */   private static Logger logger = null;
/*     */ 
/*     */   
/*     */   static {
/*     */     try {
/*  31 */       String logXML = LoggerUtil.getLoggerUtil();
/*  32 */       LoggerContext ctx = Configurator.initialize(null, (new File(logXML)).toURI().toString());
/*  33 */       logger = ctx.getLogger(ItemToSapUpdate.class.getName());
/*  34 */     } catch (Exception ex) {
/*  35 */       ex.printStackTrace();
/*     */     } 
/*     */   }
/*     */   
/*     */   public ActionResult doAction(IAgileSession iAgileSession, INode iNode, IDataObject iDataObject) {
/*  40 */     IChange change = (IChange)iDataObject;
/*  41 */     List<String> error = new ArrayList<>();
/*  42 */     List<String> success = new ArrayList<>();
/*  43 */     Map<String, List<String>> itemResultMaps = null;
/*     */     try {
/*  45 */       itemResultMaps = addItem(change, iAgileSession);
/*  46 */     } catch (APIException e) {
/*  47 */       throw new RuntimeException(e);
/*     */     } 
/*  49 */     List<String> createSuccess = itemResultMaps.get("S");
/*  50 */     List<String> createError = itemResultMaps.get("E");
/*  51 */     if (createSuccess != null && createSuccess.size() > 0) {
/*  52 */       logger.info("当前变更单物料抛送SAP成功数据有:{}", createSuccess);
/*  53 */       success.add("物料抛送成功:" + (String)createSuccess.stream().collect(Collectors.joining(",")));
/*     */     } 
/*     */     
/*  56 */     if (createError != null && createError.size() > 0) {
/*  57 */       logger.error("物料抛送SAP失败数据有:{}", createSuccess);
/*  58 */       String reason = createError.stream().collect(Collectors.joining(","));
/*  59 */       error.add("物料抛送失败:" + reason);
/*     */     } 
/*  61 */     if (error.size() <= 0) {
/*  62 */       return new ActionResult(0, "SAP修改成功！！！");
/*     */     }
/*  64 */     throw new RuntimeException((String)error.stream().collect(Collectors.joining(" | ")));
/*     */   }
/*     */ 
/*     */ 
/*     */ 
/*     */   
/*     */   public static Map<String, List<String>> addItem(IChange change, IAgileSession session) throws APIException {
/*  71 */     Map<String, List<String>> map = new HashMap<>();
/*  72 */     ITable table = change.getTable(ChangeConstants.TABLE_AFFECTEDITEMS);
/*  73 */     int size = table.size();
/*  74 */     if (size <= 0) {
/*  75 */       logger.error("当前变更单没有受影响物件");
/*     */     }
/*  77 */     List<Map<String, Object>> itemAddOrUpdateList = new ArrayList<>();
/*  78 */     for (Object o : table) {
/*  79 */       IRow row = (IRow)o;
/*  80 */       IItem item = (IItem)row.getReferent();
/*  81 */       String isMdm = item.getCell(ItemConstants.ATT_PAGE_TWO_LIST01).getValue().toString();
/*  82 */       String apiName = item.getAgileClass().getSuperClass().getAPIName();
/*  83 */       if (apiName.equals("PartsClass") && !isMdm.equals("Y")) {
/*  84 */         addSapItem(item, row, itemAddOrUpdateList, "02");
/*     */       }
/*     */     } 
/*     */ 
/*     */     
/*  89 */     logger.info("当前新增或修改物料:" + JSON.toJSON(itemAddOrUpdateList));
/*  90 */     System.out.println("itemAddOrUpdateList = " + JSON.toJSON(itemAddOrUpdateList));
/*  91 */     if (itemAddOrUpdateList.size() > 0) {
/*     */       
/*     */       try {
/*  94 */         logger.info(">>>>>>>>>>>正在向SAP抛送数据<<<<<<<<<<<<");
/*  95 */         map = PlmRequestSap.plmToSapItem(itemAddOrUpdateList);
/*  96 */         List<String> success = map.get("S");
/*  97 */         logger.info("成功物料:{}", success);
/*     */         
/*  99 */         List<String> error = map.get("E");
/* 100 */         logger.info("失败物料:{}", error);
/*     */       }
/* 102 */       catch (Exception e) {
/* 103 */         e.printStackTrace();
/* 104 */         throw new RuntimeException(e.getMessage(), e);
/*     */       } 
/*     */     }
/*     */     
/* 108 */     return map;
/*     */   }
/*     */ 
/*     */   
/*     */   private static void addSapItem(IItem item, IRow row, List<Map<String, Object>> itemAddList, String addOrUpdate) {
/*     */     try {
/* 114 */       String value = AgileUtil.getValue((IDataObject)item, ItemConstants.ATT_PAGE_TWO_MULTILIST02);
/* 115 */       String[] split = value.split(";");
/* 116 */       String werks = AgileUtil.getRedlineValueByItem(item, Integer.valueOf(2090));
/* 117 */       String[] werkList = werks.split(";");
/* 118 */       for (String werk : werkList) {
/* 119 */         Map<String, Object> sapItem = new HashMap<>();
/*     */ 
/*     */ 
/*     */ 
/*     */         
/* 124 */         sapItem.put("ZCZLX", addOrUpdate);
/*     */ 
/*     */ 
/*     */         
/* 128 */         String desc = AgileUtil.getValue((IDataObject)item, Integer.valueOf(1002));
/* 129 */         sapItem.put("ZCWB", desc);
/*     */ 
/*     */         
/* 132 */         String itemType = AgileUtil.getRedlineListApi(item, Integer.valueOf(1274));
/* 133 */         sapItem.put("MTART", itemType);
/*     */ 
/*     */ 
/*     */         
/* 137 */         String itemNumber = item.getName();
/*     */         
/* 139 */         if (!addOrUpdate.equals("01")) {
/* 140 */           sapItem.put("MATNR", itemNumber);
/* 141 */           String itemOldNumber = AgileUtil.getValue((IDataObject)item, Integer.valueOf(2011));
/* 142 */           sapItem.put("BISMT_T", itemOldNumber);
/* 143 */           sapItem.put("BISMT_X", "X");
/*     */         }
/*     */         else {
/*     */           
/* 147 */           sapItem.put("BISMT", itemNumber);
/*     */         } 
/*     */ 
/*     */ 
/*     */ 
/*     */         
/* 153 */         String lev = AgileUtil.getRedlineListApi(item, Integer.valueOf(1275));
/* 154 */         sapItem.put("PRDHA", lev);
/*     */ 
/*     */ 
/*     */         
/* 158 */         String name = item.getAgileClass().getName();
/* 159 */         String matkl = ItemToMdmTestPx.subFirstN(name, 5);
/*     */         
/* 161 */         sapItem.put("MATKL", matkl);
/*     */ 
/*     */         
/* 164 */         String mein = AgileUtil.getValue((IDataObject)item, Integer.valueOf(1271));
/*     */         
/* 166 */         sapItem.put("MEINS", mein);
/*     */ 
/*     */ 
/*     */         
/* 170 */         String lifeType = row.getCell(Integer.valueOf(1057)).getValue().toString();
/*     */ 
/*     */ 
/*     */ 
/*     */         
/* 175 */         if (lifeType.equals("停用") || lifeType.equals("EOL")) {
/* 176 */           sapItem.put("ZZT", "ZTT");
/*     */         } else {
/* 178 */           sapItem.put("ZZT", "ZSC");
/*     */         } 
/*     */ 
/*     */         
/* 182 */         logger.info("werks{}", werk);
/* 183 */         sapItem.put("WERKS", werk);
/*     */ 
/*     */         
/* 186 */         if (lifeType.equals("EV") || lifeType.equals("DV") || lifeType.equals("PV")) {
/* 187 */           sapItem.put("STAGE", "A");
/* 188 */         } else if (lifeType.equals("MP")) {
/* 189 */           sapItem.put("STAGE", "B");
/*     */         } 
/*     */         
/* 192 */         sapItem.put("ZKPDW", "个");
/* 193 */         sapItem.put("ZZZPL", "物料组");
/*     */ 
/*     */         
/* 196 */         itemAddList.add(sapItem);
/*     */       }
/*     */     
/* 199 */     } catch (Exception e) {
/* 200 */       e.printStackTrace();
/* 201 */       throw new RuntimeException("获取属性异常", e);
/*     */     } 
/*     */   }
/*     */ }


/* Location:              C:\Users\dingj\Desktop\in\123\!\com\jiexin\plm\px\ItemToSapUpdate.class
 * Java compiler version: 8 (52.0)
 * JD-Core Version:       1.1.3
 */