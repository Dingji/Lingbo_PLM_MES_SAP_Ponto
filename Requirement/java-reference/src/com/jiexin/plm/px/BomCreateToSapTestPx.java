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
/*     */ import com.alibaba.fastjson.JSON;
/*     */ import com.jiexin.plm.util.AgileUtils;
/*     */ import com.jiexin.plm.util.StringUtil;
/*     */ import java.util.ArrayList;
/*     */ import java.util.HashMap;
/*     */ import java.util.List;
/*     */ import java.util.Map;
/*     */ import org.apache.logging.log4j.core.LoggerContext;
/*     */ 
/*     */ public class BomCreateToSapTestPx implements ICustomAction {
/*  22 */   private static Logger logger = null;
/*     */ 
/*     */   
/*     */   static {
/*     */     try {
/*  27 */       String logXML = LoggerUtil.getLoggerUtil();
/*  28 */       LoggerContext ctx = Configurator.initialize(null, (new File(logXML)).toURI().toString());
/*  29 */       logger = ctx.getLogger(BomCreateToSapTestPx.class.getName());
/*  30 */     } catch (Exception ex) {
/*  31 */       ex.printStackTrace();
/*     */     } 
/*     */   }
/*     */ 
/*     */   
/*     */   public ActionResult doAction(IAgileSession iAgileSession, INode iNode, IDataObject iDataObject) {
/*  37 */     IChange change = (IChange)iDataObject;
/*     */     try {
/*  39 */       addItem(change);
/*  40 */     } catch (Exception e) {
/*  41 */       e.printStackTrace();
/*  42 */       return new ActionResult(-1, e);
/*     */     } 
/*     */     
/*  45 */     return new ActionResult(0, "发送物料信息返回信息");
/*     */   }
/*     */ 
/*     */   
/*     */   public static Map<String, List<String>> addItem(IChange change) throws APIException {
/*  50 */     Map<String, List<String>> map = new HashMap<>();
/*  51 */     ITable table = change.getTable(ChangeConstants.TABLE_AFFECTEDITEMS);
/*  52 */     int size = table.size();
/*  53 */     if (size <= 0) {
/*  54 */       logger.error("当前变更单没有受影响物件");
/*     */     }
/*  56 */     List<Map<String, String>> itemAddBomList = new ArrayList<>();
/*     */ 
/*     */     
/*  59 */     for (Object o : table) {
/*  60 */       IRow row = (IRow)o;
/*  61 */       IItem item = (IItem)row.getReferent();
/*  62 */       String apiName = item.getAgileClass().getSuperClass().getAPIName();
/*  63 */       if (apiName.equals("PartsClass")) {
/*     */         
/*  65 */         String s = row.getCell(ChangeConstants.ATT_AFFECTED_ITEMS_OLD_REV).getValue().toString();
/*  66 */         logger.info("当前物料版本为：" + s);
/*  67 */         String bomWerks = item.getCell(ItemConstants.ATT_PAGE_TWO_MULTILIST02).getValue().toString();
/*  68 */         if (null == s || s.trim().equals("")) {
/*  69 */           List<IRow> list = AgileUtils.getBomList(item);
/*  70 */           addNewCreateBom(item, list, itemAddBomList, change); continue;
/*     */         } 
/*  72 */         List<IRow> nowBomList = AgileUtils.getOldBom(item);
/*  73 */         logger.info(Integer.valueOf(nowBomList.size()));
/*  74 */         if (nowBomList.size() > 0) {
/*     */           continue;
/*     */         }
/*     */         
/*  78 */         List<IRow> bomList = AgileUtils.getBomList(item);
/*  79 */         addNewCreateBom(item, bomList, itemAddBomList, change);
/*     */       } 
/*     */     } 
/*     */ 
/*     */ 
/*     */     
/*  85 */     logger.info("抛送创建BOM：" + itemAddBomList);
/*  86 */     System.out.println("抛送创建BOM:" + JSON.toJSONString(itemAddBomList));
/*  87 */     if (itemAddBomList.size() > 0) {
/*     */       try {
/*  89 */         map = PlmBomToSap.plmToSapBom(itemAddBomList);
/*  90 */       } catch (Exception e) {
/*  91 */         e.printStackTrace();
/*  92 */         throw new RuntimeException("抛送物料创建BOM时异常！！！", e);
/*     */       } 
/*     */     }
/*  95 */     return map;
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
/*     */   private static void addNewCreateBom(IItem item, List<IRow> newBomList, List<Map<String, String>> itemAddBomList, IChange change) throws APIException {
/* 107 */     String partNumber = item.getName();
/* 108 */     for (IRow iRow : newBomList) {
/* 109 */       IItem iItem = (IItem)iRow.getReferent();
/* 110 */       String tdsx = iRow.getCell(Integer.valueOf(2000019515)).getValue().toString();
/* 111 */       if (!StringUtil.isEmpty(tdsx) && !tdsx.equals("1")) {
/*     */         continue;
/*     */       }
/* 114 */       Map<String, String> bom = new HashMap<>();
/* 115 */       bom.put("PARENTNUM", partNumber);
/*     */       
/* 117 */       String itemNumber = iItem.getName();
/* 118 */       bom.put("SUBNUM", itemNumber);
/*     */ 
/*     */       
/* 121 */       String string = iItem.getCell(Integer.valueOf(1274)).getValue().toString();
/* 122 */       if (string.equals("客供物料类型")) {
/* 123 */         bom.put("ZKGL", "X");
/*     */       } else {
/* 125 */         bom.put("ZKGL", "");
/*     */       } 
/*     */       
/* 128 */       String qty = iRow.getCell(ItemConstants.ATT_BOM_QTY).getValue().toString();
/* 129 */       System.out.println("qty = " + qty);
/* 130 */       bom.put("SUBQUANTITY", qty);
/*     */ 
/*     */       
/* 133 */       String unit = iRow.getCell(ItemConstants.ATT_BOM_ITEM_LIST11).getValue().toString();
/* 134 */       bom.put("SUBUNIT", unit);
/*     */ 
/*     */       
/* 137 */       String lineNum = iRow.getCell(ItemConstants.ATT_BOM_FIND_NUM).getValue().toString();
/* 138 */       bom.put("LINENUM", lineNum);
/*     */ 
/*     */ 
/*     */       
/* 142 */       String factoryNum = item.getCell(ItemConstants.ATT_PAGE_TWO_LIST02).getValue().toString();
/* 143 */       bom.put("FACTORYNUM", factoryNum);
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
/*     */       
/* 155 */       itemAddBomList.add(bom);
/*     */     } 
/*     */   }
/*     */ }


/* Location:              {{SOURCE_PATH}}\com\jiexin\plm\px\BomCreateToSapTestPx.class
 * Java compiler version: 8 (52.0)
 * JD-Core Version:       1.1.3
 */