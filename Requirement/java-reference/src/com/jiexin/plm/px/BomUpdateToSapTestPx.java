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
/*     */ import com.jiexin.plm.util.AgileUtils;
/*     */ import com.jiexin.plm.util.StringUtil;
/*     */ import java.time.LocalDate;
/*     */ import java.time.format.DateTimeFormatter;
/*     */ import java.util.ArrayList;
/*     */ import java.util.HashMap;
/*     */ import java.util.List;
/*     */ import java.util.Map;
/*     */ import org.apache.logging.log4j.core.LoggerContext;
/*     */ 
/*     */ public class BomUpdateToSapTestPx implements ICustomAction {
/*  23 */   private static Logger logger = null;
/*     */ 
/*     */   
/*     */   static {
/*     */     try {
/*  28 */       String logXML = LoggerUtil.getLoggerUtil();
/*  29 */       LoggerContext ctx = Configurator.initialize(null, (new File(logXML)).toURI().toString());
/*  30 */       logger = ctx.getLogger(BomUpdateToSapTestPx.class.getName());
/*  31 */     } catch (Exception ex) {
/*  32 */       ex.printStackTrace();
/*     */     } 
/*     */   }
/*     */ 
/*     */   
/*     */   public ActionResult doAction(IAgileSession iAgileSession, INode iNode, IDataObject iDataObject) {
/*  38 */     IChange change = (IChange)iDataObject;
/*     */     try {
/*  40 */       addItem(change);
/*  41 */     } catch (Exception e) {
/*  42 */       e.printStackTrace();
/*  43 */       return new ActionResult(-1, e);
/*     */     } 
/*     */     
/*  46 */     return new ActionResult(0, "发送物料信息返回信息");
/*     */   }
/*     */ 
/*     */   
/*     */   public static Map<String, List<String>> addItem(IChange change) throws APIException {
/*  51 */     Map<String, List<String>> map = new HashMap<>();
/*  52 */     ITable table = change.getTable(ChangeConstants.TABLE_AFFECTEDITEMS);
/*  53 */     int size = table.size();
/*  54 */     if (size <= 0) {
/*  55 */       logger.error("当前变更单没有受影响物件");
/*     */     }
/*     */ 
/*     */     
/*  59 */     List<Map<String, String>> itemUpdateBomList = new ArrayList<>();
/*  60 */     for (Object o : table) {
/*  61 */       IRow row = (IRow)o;
/*  62 */       IItem item = (IItem)row.getReferent();
/*  63 */       String apiName = item.getAgileClass().getSuperClass().getAPIName();
/*  64 */       if (apiName.equals("PartsClass")) {
/*     */         
/*  66 */         String s = row.getCell(ChangeConstants.ATT_AFFECTED_ITEMS_OLD_REV).getValue().toString();
/*  67 */         List<IRow> bomList = AgileUtils.getOldBom(item);
/*  68 */         if (!s.isEmpty() && bomList.size() > 0) {
/*  69 */           addUpdateBom(item, change, itemUpdateBomList);
/*     */         }
/*     */       } 
/*     */     } 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */     
/*  78 */     logger.info("抛送修改BOM：" + itemUpdateBomList);
/*  79 */     System.out.println("抛送修改BOM：:" + JSON.toJSONString(itemUpdateBomList));
/*     */     
/*  81 */     if (itemUpdateBomList.size() > 0) {
/*     */       try {
/*  83 */         map = PlmBomToSapUpdate.plmToSapBom(itemUpdateBomList);
/*  84 */       } catch (Exception e) {
/*  85 */         e.printStackTrace();
/*  86 */         throw new RuntimeException("抛送物料修改BOM时异常！！！", e);
/*     */       } 
/*     */     }
/*  89 */     return map;
/*     */   }
/*     */ 
/*     */   
/*     */   public static void main(String[] args) throws APIException {
/*  94 */     IAgileSession session = SessionFactory.getSession();
/*  95 */     IChange change = (IChange)session.getObject(3, "E-0003598");
/*  96 */     ITable table = change.getTable(ChangeConstants.TABLE_AFFECTEDITEMS);
/*  97 */     List<Map<String, String>> itemUpdateBomList = new ArrayList<>();
/*  98 */     for (Object o : table) {
/*  99 */       IRow row = (IRow)o;
/* 100 */       IItem item = (IItem)row.getReferent();
/* 101 */       String apiName = item.getAgileClass().getSuperClass().getAPIName();
/* 102 */       if (apiName.equals("PartsClass")) {
/*     */         
/* 104 */         String s = row.getCell(ChangeConstants.ATT_AFFECTED_ITEMS_OLD_REV).getValue().toString();
/* 105 */         List<IRow> bomList = AgileUtils.getBomList(item);
/* 106 */         if (!s.isEmpty() && bomList.size() > 0) {
/* 107 */           addUpdateBom(item, change, itemUpdateBomList);
/*     */         }
/*     */       } 
/*     */     } 
/* 111 */     System.out.println("itemUpdateBomList = " + itemUpdateBomList);
/*     */   }
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */   
/*     */   private static void addUpdateBom(IItem item, IChange change, List<Map<String, String>> itemUpdateBomList) throws APIException {
/* 123 */     ITable table = item.getTable(ItemConstants.TABLE_REDLINEBOM);
/* 124 */     List<IRow> oldBom = AgileUtils.getLastRevBomList(item, change);
/* 125 */     for (Object o : table) {
/* 126 */       IRow iRow = (IRow)o;
/* 127 */       System.out.println("iRow = " + iRow);
/* 128 */       IItem iItem = (IItem)iRow.getReferent();
/* 129 */       Map<String, String> bom = new HashMap<>();
/* 130 */       String partNumber = item.getName();
/* 131 */       bom.put("PARENTNUM", partNumber);
/*     */       
/* 133 */       String itemNumber = iRow.getCell(ItemConstants.ATT_BOM_ITEM_NUMBER).getValue().toString();
/* 134 */       System.out.println("itemNumber = " + itemNumber);
/* 135 */       bom.put("SUBNUM", itemNumber);
/*     */ 
/*     */       
/* 138 */       String qty = iRow.getCell(ItemConstants.ATT_BOM_QTY).getValue().toString();
/* 139 */       System.out.println("BOM修改   qty = " + qty);
/* 140 */       bom.put("SUBQUANTITY", qty);
/*     */ 
/*     */ 
/*     */       
/* 144 */       String unit = iRow.getCell(ItemConstants.ATT_BOM_ITEM_LIST11).getValue().toString();
/* 145 */       bom.put("SUBUNIT", unit);
/* 146 */       String string = iItem.getCell(Integer.valueOf(1274)).getValue().toString();
/* 147 */       if (string.equals("客供物料类型")) {
/* 148 */         bom.put("ZKGL", "X");
/*     */       } else {
/* 150 */         bom.put("ZKGL", "");
/*     */       } 
/*     */       
/* 153 */       String lineNum = iRow.getCell(ItemConstants.ATT_BOM_FIND_NUM).getValue().toString();
/* 154 */       bom.put("LINENUM", lineNum);
/*     */ 
/*     */       
/* 157 */       String factoryNum = item.getCell(ItemConstants.ATT_PAGE_TWO_LIST02).getValue().toString();
/* 158 */       bom.put("FACTORYNUM", factoryNum);
/*     */ 
/*     */       
/* 161 */       String tdsx = iRow.getCell(Integer.valueOf(2000019515)).getValue().toString();
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */       
/* 167 */       String changeName = change.getCell(ChangeConstants.ATT_COVER_PAGE_NUMBER).getValue().toString();
/* 168 */       bom.put("MCNNUM", changeName);
/* 169 */       LocalDate currentDate = LocalDate.now();
/*     */ 
/*     */       
/* 172 */       DateTimeFormatter formatter = DateTimeFormatter.ofPattern("yyyyMMdd");
/*     */ 
/*     */       
/* 175 */       String dateStr = currentDate.format(formatter);
/* 176 */       bom.put("EFFECTIVEDATE", dateStr);
/*     */       
/* 178 */       String reason = change.getCell(ChangeConstants.ATT_COVER_PAGE_REASON_FOR_CHANGE).getValue().toString();
/* 179 */       bom.put("AETXT", reason);
/* 180 */       bom.put("SWITCHINGMODE", "01");
/*     */ 
/*     */ 
/*     */       
/* 184 */       if (iRow.isFlagSet(ItemConstants.FLAG_IS_REDLINE_ADDED)) {
/* 185 */         bom.put("OPERATIONTYPE", "01");
/*     */         
/* 187 */         if (!StringUtil.isEmpty(tdsx) && !tdsx.equals("1")) {
/*     */           continue;
/*     */         }
/* 190 */         itemUpdateBomList.add(bom); continue;
/*     */       } 
/* 192 */       if (iRow.isFlagSet(ItemConstants.FLAG_IS_REDLINE_REMOVED)) {
/*     */         
/* 194 */         if (!StringUtil.isEmpty(tdsx) && !tdsx.equals("1")) {
/*     */           continue;
/*     */         }
/* 197 */         bom.put("OPERATIONTYPE", "03");
/* 198 */         bom.put("SUBNUM", iItem.getName());
/*     */         
/* 200 */         itemUpdateBomList.add(bom); continue;
/*     */       } 
/* 202 */       if (iRow.isFlagSet(ItemConstants.FLAG_IS_REDLINE_MODIFIED)) {
/* 203 */         if (!StringUtil.isEmpty(tdsx)) {
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */           
/* 213 */           List<IRow> collect = (List<IRow>)oldBom.stream().filter(obj -> { try { return obj.getCell(ItemConstants.ATT_BOM_ITEM_NUMBER).getValue().toString().equals(itemNumber); } catch (APIException e) { e.printStackTrace(); throw new RuntimeException(e); }  }).collect(Collectors.toList());
/*     */           
/* 215 */           if (collect.size() > 0) {
/* 216 */             IRow row = collect.get(0);
/* 217 */             String oldTdsx = row.getCell(Integer.valueOf(2000019515)).getValue().toString();
/* 218 */             if (oldTdsx.isEmpty()) {
/*     */               
/* 220 */               if (!tdsx.equals("1")) {
/* 221 */                 bom.put("OPERATIONTYPE", "03");
/*     */               }
/*     */               else {
/*     */                 
/* 225 */                 bom.put("OPERATIONTYPE", "02");
/*     */               
/*     */               }
/*     */             
/*     */             }
/* 230 */             else if (oldTdsx.equals("1")) {
/* 231 */               if (!tdsx.equals("1")) {
/* 232 */                 bom.put("OPERATIONTYPE", "03");
/*     */               } else {
/* 234 */                 bom.put("OPERATIONTYPE", "02");
/*     */               }
/*     */             
/* 237 */             } else if (tdsx.equals("1")) {
/* 238 */               bom.put("OPERATIONTYPE", "01");
/*     */             } else {
/*     */               
/*     */               continue;
/*     */             } 
/*     */           } 
/*     */         } else {
/*     */           
/* 246 */           bom.put("OPERATIONTYPE", "02");
/*     */         } 
/* 248 */         itemUpdateBomList.add(bom);
/*     */       } 
/*     */     } 
/*     */   }
/*     */ }


/* Location:              {{SOURCE_PATH}}\com\jiexin\plm\px\BomUpdateToSapTestPx.class
 * Java compiler version: 8 (52.0)
 * JD-Core Version:       1.1.3
 */