/*     */ package com.jiexin.plm.px;
/*     */ import com.agile.api.APIException;
/*     */ import com.agile.api.ChangeConstants;
/*     */ import com.agile.api.IAgileList;
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
/*     */ import java.util.Arrays;
/*     */ import java.util.HashMap;
/*     */ import java.util.List;
/*     */ import java.util.Map;
/*     */ import java.util.stream.Collectors;
/*     */ 
/*     */ public class ItemToSapTestPx implements ICustomAction {
/*  23 */   private static Logger logger = null;
/*     */ 
/*     */   
/*     */   static {
/*     */     try {
/*  28 */       String logXML = LoggerUtil.getLoggerUtil();
/*     */       
/*  30 */       LoggerContext ctx = Configurator.initialize(null, (new File(logXML)).toURI().toString());
/*  31 */       logger = ctx.getLogger(ItemToSapTestPx.class.getName());
/*  32 */     } catch (Exception ex) {
/*  33 */       ex.printStackTrace();
/*     */     } 
/*     */   }
/*     */ 
/*     */   
/*     */   public ActionResult doAction(IAgileSession iAgileSession, INode iNode, IDataObject iDataObject) {
/*  39 */     IChange change = (IChange)iDataObject;
/*     */     try {
/*  41 */       List<String> error = new ArrayList<>();
/*  42 */       List<String> success = new ArrayList<>();
/*  43 */       Map<String, List<String>> itemResultMaps = addItem(change, iAgileSession);
/*  44 */       List<String> createSuccess = itemResultMaps.get("S");
/*  45 */       List<String> createError = itemResultMaps.get("E");
/*  46 */       if (createSuccess != null && createSuccess.size() > 0) {
/*  47 */         logger.info("当前变更单物料抛送SAP成功数据有:{}", createSuccess);
/*  48 */         success.add("物料抛送成功:" + (String)createSuccess.stream().collect(Collectors.joining(",")));
/*     */       } 
/*     */       
/*  51 */       if (createError != null && createError.size() > 0) {
/*  52 */         logger.error("物料抛送SAP失败数据有:{}", createSuccess);
/*  53 */         String reason = createError.stream().collect(Collectors.joining(","));
/*  54 */         error.add("物料抛送失败:" + reason);
/*     */       } 
/*  56 */       Map<String, List<String>> resultBomCreate = BomCreateToSapTestPx.addItem(change);
/*  57 */       List<String> createSuccessBom = resultBomCreate.get("S");
/*  58 */       List<String> createErrorBom = resultBomCreate.get("E");
/*  59 */       if (createSuccessBom != null && createSuccessBom.size() > 0) {
/*  60 */         logger.info("当前变更单物料抛送SAP成功数据有:{}", createSuccessBom);
/*  61 */         success.add("物料BOM创建按成功:" + (String)createSuccessBom.stream().collect(Collectors.joining(",")));
/*     */       } 
/*     */       
/*  64 */       if (createErrorBom != null && createErrorBom.size() > 0) {
/*  65 */         logger.error("当前变更单物料抛送SAP失败数据有:{}", createErrorBom);
/*  66 */         String reason = createErrorBom.stream().collect(Collectors.joining(","));
/*  67 */         error.add("物料BOM新增失败:" + reason);
/*     */       } 
/*     */       
/*  70 */       Map<String, List<String>> resultBomUpdate = BomUpdateToSapTestPx.addItem(change);
/*     */       
/*  72 */       List<String> updateSuccessBom = resultBomUpdate.get("S");
/*  73 */       List<String> updateErrorBom = resultBomUpdate.get("E");
/*  74 */       if (resultBomUpdate != null && resultBomUpdate.size() > 0) {
/*  75 */         logger.info("当前变更单物料抛送SAP成功数据有:{}", updateSuccessBom);
/*  76 */         success.add("物料BOM更新成功:" + (String)updateSuccessBom.stream().collect(Collectors.joining(",")));
/*     */       } 
/*     */       
/*  79 */       if (updateErrorBom != null && updateErrorBom.size() > 0) {
/*  80 */         logger.error("当前变更单物料抛送SAP失败数据有:{}", updateErrorBom);
/*  81 */         String reason = updateErrorBom.stream().collect(Collectors.joining(","));
/*  82 */         error.add("物料BOM更新失败:" + reason);
/*     */       } 
/*  84 */       if (error.size() <= 0) {
/*  85 */         return new ActionResult(0, "SAP集成成功！！！");
/*     */       }
/*  87 */       throw new RuntimeException((String)error.stream().collect(Collectors.joining(" | ")));
/*     */     }
/*  89 */     catch (Exception e) {
/*  90 */       e.printStackTrace();
/*  91 */       return new ActionResult(-1, e);
/*     */     } 
/*     */   }
/*     */ 
/*     */   
/*     */   public static Map<String, List<String>> addItem(IChange change, IAgileSession session) throws APIException {
/*  97 */     Map<String, List<String>> map = new HashMap<>();
/*  98 */     ITable table = change.getTable(ChangeConstants.TABLE_AFFECTEDITEMS);
/*  99 */     int size = table.size();
/* 100 */     if (size <= 0) {
/* 101 */       logger.error("当前变更单没有受影响物件");
/*     */     }
/* 103 */     List<Map<String, Object>> itemAddOrUpdateList = new ArrayList<>();
/* 104 */     for (Object o : table) {
/* 105 */       IRow row = (IRow)o;
/* 106 */       IItem item = (IItem)row.getReferent();
/* 107 */       String isMdm = item.getCell(ItemConstants.ATT_PAGE_TWO_LIST01).getValue().toString();
/* 108 */       String apiName = item.getAgileClass().getSuperClass().getAPIName();
/* 109 */       if (apiName.equals("PartsClass") && !isMdm.equals("Y")) {
/* 110 */         if (StringUtil.isEmpty(row.getCell(ChangeConstants.ATT_AFFECTED_ITEMS_OLD_REV).getValue().toString())) {
/* 111 */           IAgileList value = (IAgileList)item.getValue(ItemConstants.ATT_PAGE_TWO_MULTILIST02);
/* 112 */           value.setSelection(new Object[0]);
/* 113 */           item.getCell(ItemConstants.ATT_PAGE_TWO_MULTILIST02).setValue(value);
/* 114 */           addSapItem(item, row, itemAddOrUpdateList, "01"); continue;
/*     */         } 
/* 116 */         addSapItem(item, row, itemAddOrUpdateList, "02");
/*     */       } 
/*     */     } 
/*     */ 
/*     */     
/* 121 */     logger.info("当前新增或修改物料:" + JSON.toJSON(itemAddOrUpdateList));
/* 122 */     System.out.println("itemAddOrUpdateList = " + JSON.toJSON(itemAddOrUpdateList));
/* 123 */     if (itemAddOrUpdateList.size() > 0) {
/*     */       
/*     */       try {
/* 126 */         logger.info(">>>>>>>>>>>正在向SAP抛送数据<<<<<<<<<<<<");
/* 127 */         map = PlmRequestSap.plmToSapItem(itemAddOrUpdateList);
/* 128 */         List<String> success = map.get("S");
/* 129 */         logger.info("成功物料:{}", success);
/* 130 */         for (String s : success) {
/* 131 */           IItem object = (IItem)session.getObject(2, s);
/* 132 */           String oldWerk = object.getCell(ItemConstants.ATT_PAGE_TWO_MULTILIST02).getValue().toString();
/* 133 */           List<String> list1 = Arrays.asList(oldWerk.split(";"));
/* 134 */           String newWerk = object.getCell(ItemConstants.ATT_PAGE_TWO_MULTILIST01).getValue().toString();
/* 135 */           String[] split = newWerk.split(";");
/* 136 */           List<String> list = Arrays.asList(split);
/* 137 */           List<String> add = new ArrayList<>();
/* 138 */           for (String a : list) {
/* 139 */             if (!list1.contains(a) && 
/* 140 */               !a.trim().equals("")) {
/* 141 */               add.add(a);
/*     */             }
/*     */           } 
/*     */           
/* 145 */           add.addAll(list1);
/* 146 */           List<String> collect = (List<String>)add.stream().filter(obj -> !obj.trim().equals("")).collect(Collectors.toList());
/* 147 */           System.out.println(collect);
/* 148 */           IAgileList value = (IAgileList)object.getValue(ItemConstants.ATT_PAGE_TWO_MULTILIST02);
/* 149 */           value.setSelection(collect.toArray());
/* 150 */           object.getCell(ItemConstants.ATT_PAGE_TWO_MULTILIST02).setValue(value);
/*     */         } 
/* 152 */         List<String> error = map.get("E");
/* 153 */         logger.info("失败物料:{}", error);
/*     */       }
/* 155 */       catch (Exception e) {
/* 156 */         e.printStackTrace();
/* 157 */         throw new RuntimeException(e.getMessage(), e);
/*     */       } 
/*     */     }
/*     */     
/* 161 */     return map;
/*     */   }
/*     */ 
/*     */ 
/*     */   
/*     */   public static void addSapItem(IItem item, IRow row, List<Map<String, Object>> itemAddList, String addOrUpdate) {
/*     */     try {
/* 168 */       String werks = AgileUtil.getRedlineValueByItem(item, Integer.valueOf(2090));
/* 169 */       String[] werkList = werks.split(";");
/* 170 */       for (String werk : werkList) {
/* 171 */         Map<String, Object> sapItem = new HashMap<>();
/*     */ 
/*     */ 
/*     */         
/* 175 */         sapItem.put("ZCZLX", addOrUpdate);
/*     */ 
/*     */ 
/*     */         
/* 179 */         String desc = AgileUtil.getValue((IDataObject)item, Integer.valueOf(1002));
/* 180 */         sapItem.put("ZCWB", desc);
/*     */ 
/*     */         
/* 183 */         String itemType = AgileUtil.getRedlineListApi(item, Integer.valueOf(1274));
/* 184 */         sapItem.put("MTART", itemType);
/*     */ 
/*     */ 
/*     */         
/* 188 */         String itemNumber = item.getName();
/* 189 */         String itemOldNumber = AgileUtil.getValue((IDataObject)item, Integer.valueOf(2011));
/*     */ 
/*     */         
/* 192 */         if (!addOrUpdate.equals("01")) {
/* 193 */           sapItem.put("MATNR", itemNumber);
/* 194 */           sapItem.put("BISMT", itemOldNumber);
/*     */           
/* 196 */           String value = AgileUtil.getValue((IDataObject)item, ItemConstants.ATT_PAGE_TWO_MULTILIST02);
/* 197 */           List<String> split = Arrays.asList(value.split(";"));
/* 198 */           if (!split.contains(werk)) {
/* 199 */             sapItem.put("MATNR", itemNumber);
/* 200 */             sapItem.put("BISMT", itemNumber);
/* 201 */             sapItem.put("ZCZLX", "01");
/* 202 */             if (!itemOldNumber.isEmpty()) {
/* 203 */               sapItem.put("BISMT_T", itemOldNumber);
/* 204 */               sapItem.put("BISMT_X", "X");
/*     */             } 
/*     */           } else {
/* 207 */             sapItem.put("BISMT", itemNumber);
/* 208 */             sapItem.put("BISMT_T", itemOldNumber);
/* 209 */             sapItem.put("BISMT_X", "X");
/*     */           }
/*     */         
/*     */         }
/*     */         else {
/*     */           
/* 215 */           if (werkList.length > 1) {
/* 216 */             List<Map<String, Object>> collect = (List<Map<String, Object>>)itemAddList.stream().filter(obj -> (obj.get("BISMT").equals(itemNumber) && obj.get("ZCZLX").equals("01"))).collect(Collectors.toList());
/* 217 */             if (collect.size() <= 0) {
/* 218 */               sapItem.put("BISMT", itemNumber);
/*     */             } else {
/* 220 */               sapItem.put("BISMT", itemNumber);
/* 221 */               sapItem.put("MATNR", itemNumber);
/*     */             } 
/*     */           } else {
/* 224 */             sapItem.put("BISMT", itemNumber);
/*     */           } 
/* 226 */           if (!itemOldNumber.isEmpty()) {
/* 227 */             sapItem.put("BISMT_T", itemOldNumber);
/* 228 */             sapItem.put("BISMT_X", "X");
/*     */           } 
/*     */         } 
/*     */ 
/*     */ 
/*     */         
/* 234 */         String lev = AgileUtil.getRedlineListApi(item, Integer.valueOf(1275));
/* 235 */         sapItem.put("PRDHA", lev);
/*     */ 
/*     */ 
/*     */         
/* 239 */         String name = item.getAgileClass().getName();
/* 240 */         String matkl = ItemToMdmTestPx.subFirstN(name, 5);
/*     */         
/* 242 */         sapItem.put("MATKL", matkl);
/*     */ 
/*     */         
/* 245 */         String mein = AgileUtil.getValue((IDataObject)item, Integer.valueOf(1271));
/*     */         
/* 247 */         sapItem.put("MEINS", mein);
/*     */ 
/*     */ 
/*     */         
/* 251 */         String lifeType = row.getCell(Integer.valueOf(1057)).getValue().toString();
/*     */ 
/*     */ 
/*     */ 
/*     */         
/* 256 */         if (lifeType.equals("停用") || lifeType.equals("EOL")) {
/* 257 */           sapItem.put("ZZT", "ZTT");
/*     */         } else {
/* 259 */           sapItem.put("ZZT", "ZSC");
/*     */         } 
/*     */ 
/*     */         
/* 263 */         logger.info("werks{}", werk);
/* 264 */         sapItem.put("WERKS", werk);
/*     */ 
/*     */         
/* 267 */         if (lifeType.equals("EV") || lifeType.equals("DV") || lifeType.equals("PV")) {
/* 268 */           sapItem.put("STAGE", "A");
/* 269 */         } else if (lifeType.equals("MP")) {
/* 270 */           sapItem.put("STAGE", "B");
/*     */         } 
/*     */         
/* 273 */         sapItem.put("ZKPDW", "个");
/* 274 */         sapItem.put("ZZZPL", "物料组");
/*     */ 
/*     */         
/* 277 */         itemAddList.add(sapItem);
/*     */       }
/*     */     
/* 280 */     } catch (Exception e) {
/* 281 */       e.printStackTrace();
/* 282 */       throw new RuntimeException("获取属性异常", e);
/*     */     } 
/*     */   }
/*     */   
/*     */   public static void main(String[] args) throws APIException {
/* 287 */     IAgileSession session = SessionFactory.getSession();
/* 288 */     IItem object = (IItem)session.getObject(2, "X9841-0000428");
/* 289 */     String oldWerk = object.getCell(ItemConstants.ATT_PAGE_TWO_MULTILIST02).getValue().toString();
/* 290 */     List<String> list1 = Arrays.asList(oldWerk.split(";"));
/* 291 */     String newWerk = object.getCell(ItemConstants.ATT_PAGE_TWO_MULTILIST01).getValue().toString();
/* 292 */     String[] split = newWerk.split(";");
/* 293 */     List<String> list = Arrays.asList(split);
/* 294 */     List<String> add = new ArrayList<>();
/* 295 */     for (String s : list) {
/* 296 */       if (!list1.contains(s) && 
/* 297 */         !s.trim().equals("")) {
/* 298 */         add.add(s.trim());
/*     */       }
/*     */     } 
/*     */     
/* 302 */     add.addAll(list1);
/* 303 */     System.out.println(add);
/*     */     
/* 305 */     IAgileList value = object.getCell(ItemConstants.ATT_PAGE_TWO_MULTILIST02).getAvailableValues();
/* 306 */     value.setSelection(add.toArray());
/*     */     
/* 308 */     object.setValue(ItemConstants.ATT_PAGE_TWO_MULTILIST02, value);
/*     */   }
/*     */ }


/* Location:              {{SOURCE_PATH}}\com\jiexin\plm\px\ItemToSapTestPx.class
 * Java compiler version: 8 (52.0)
 * JD-Core Version:       1.1.3
 */