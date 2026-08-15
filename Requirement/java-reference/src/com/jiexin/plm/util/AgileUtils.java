/*     */ package com.jiexin.plm.util;
/*     */ 
/*     */ import com.agile.api.APIException;
/*     */ import com.agile.api.IAgileList;
/*     */ import com.agile.api.IAgileSession;
/*     */ import com.agile.api.IChange;
/*     */ import com.agile.api.IItem;
/*     */ import com.agile.api.IRow;
/*     */ import com.agile.api.ITable;
/*     */ import com.agile.api.ItemConstants;
/*     */ import java.util.ArrayList;
/*     */ import java.util.Iterator;
/*     */ import java.util.List;
/*     */ 
/*     */ 
/*     */ public class AgileUtils
/*     */ {
/*     */   public static List<IRow> getBomList(IItem item) throws APIException {
/*     */     try {
/*  20 */       List<IRow> boms = new ArrayList<>();
/*  21 */       ITable table = item.getTable(ItemConstants.TABLE_REDLINEBOM);
/*  22 */       for (Object o : table) {
/*  23 */         IRow row = (IRow)o;
/*  24 */         boms.add(row);
/*     */       } 
/*  26 */       return boms;
/*  27 */     } catch (Exception e) {
/*  28 */       e.printStackTrace();
/*  29 */       throw new RuntimeException("获取当前物料：" + item.getName() + "BOM时出现异常", e);
/*     */     } 
/*     */   }
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */   
/*     */   public static List<IRow> getNowBomList(IItem item) throws APIException {
/*     */     try {
/*  41 */       List<IRow> boms = new ArrayList<>();
/*  42 */       ITable table = item.getTable(ItemConstants.TABLE_BOM);
/*  43 */       for (Object o : table) {
/*  44 */         IRow row = (IRow)o;
/*  45 */         boms.add(row);
/*     */       } 
/*  47 */       return boms;
/*     */     }
/*  49 */     catch (Exception e) {
/*  50 */       e.printStackTrace();
/*  51 */       throw new RuntimeException("获取当前物料：" + item.getName() + "BOM时出现异常", e);
/*     */     } 
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
/*     */   public static List<IRow> getLastRevBomList(IItem item, IChange change) throws APIException {
/*     */     try {
/*  65 */       IChange lastRevChange = getLastRevChange(item);
/*  66 */       List<IRow> boms = new ArrayList<>();
/*  67 */       if (lastRevChange == null) {
/*  68 */         item.setRevision("初始");
/*  69 */         ITable table = item.getTable(ItemConstants.TABLE_BOM);
/*  70 */         for (Object o : table) {
/*  71 */           IRow row = (IRow)o;
/*  72 */           boms.add(row);
/*     */         } 
/*     */       } else {
/*  75 */         item.setRevision(lastRevChange);
/*  76 */         ITable table = item.getTable(ItemConstants.TABLE_REDLINEBOM);
/*  77 */         for (Object o : table) {
/*  78 */           IRow row = (IRow)o;
/*  79 */           boms.add(row);
/*     */         } 
/*     */       } 
/*  82 */       return boms;
/*  83 */     } catch (Exception e) {
/*  84 */       e.printStackTrace();
/*  85 */       throw new RuntimeException("获取当前物料：" + item.getName() + "BOM时出现异常", e);
/*     */     } finally {
/*     */       
/*  88 */       item.setRevision(change);
/*     */     } 
/*     */   }
/*     */   
/*     */   public static IChange getLastRevChange(IItem item) throws APIException {
/*     */     try {
/*  94 */       ITable historyChange = item.getTable(ItemConstants.TABLE_CHANGEHISTORY);
/*  95 */       Iterator iterator = historyChange.iterator(); if (iterator.hasNext()) { Object o = iterator.next();
/*  96 */         IRow row = (IRow)o;
/*  97 */         IChange referent = (IChange)row.getReferent();
/*  98 */         return referent; }
/*     */     
/* 100 */     } catch (Exception e) {
/* 101 */       e.printStackTrace();
/* 102 */       throw new RuntimeException("获取当前物料：" + item.getName() + "上一版本时出现异常", e);
/*     */     } 
/* 104 */     return null;
/*     */   }
/*     */   
/*     */   public static void main(String[] args) throws APIException {
/* 108 */     IAgileSession session = SessionFactory.getSession();
/* 109 */     IChange change = (IChange)session.getObject(3, "E-0003504");
/* 110 */     IItem item = (IItem)session.getObject(2, "2-S-M118DB04X-01");
/* 111 */     item.setRevision(change);
/* 112 */     ITable table = item.getTable(ItemConstants.TABLE_REDLINEBOM);
/* 113 */     for (Object o : table) {
/* 114 */       IRow row = (IRow)o;
/* 115 */       if (row.isFlagSet(ItemConstants.FLAG_IS_REDLINE_REMOVED)) {
/*     */         continue;
/*     */       }
/* 118 */       System.out.println("row = " + row.getCell(ItemConstants.ATT_BOM_FIND_NUM).getValue().toString() + "---" + row.getCell(ItemConstants.ATT_BOM_ITEM_NUMBER).getValue().toString());
/*     */     } 
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
/*     */   public static String getOneSelectApi(IItem item, Integer id) throws APIException {
/* 131 */     IAgileList value = (IAgileList)item.getValue(id);
/* 132 */     IAgileList[] arrayOfIAgileList = value.getSelection(); int i = arrayOfIAgileList.length; byte b = 0; if (b < i) { IAgileList iAgileList = arrayOfIAgileList[b];
/* 133 */       return iAgileList.getAPIName(); }
/*     */     
/* 135 */     return "";
/*     */   }
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */   
/*     */   public static List<IRow> getRevBomList(IItem item, IChange change) throws APIException {
/* 146 */     List<IRow> boms = new ArrayList<>();
/* 147 */     ITable table = item.getTable(ItemConstants.TABLE_REDLINEBOM);
/* 148 */     for (Object o : table) {
/* 149 */       IRow row = (IRow)o;
/* 150 */       boms.add(row);
/*     */     } 
/* 152 */     return boms;
/*     */   }
/*     */   
/*     */   public static List<IRow> getOldBom(IItem item) throws APIException {
/* 156 */     List<IRow> boms = new ArrayList<>();
/* 157 */     ITable table = item.getTable(ItemConstants.TABLE_REDLINEBOM);
/* 158 */     for (Object o : table) {
/* 159 */       IRow row = (IRow)o;
/*     */       
/* 161 */       if (!row.isFlagSet(ItemConstants.FLAG_IS_REDLINE_ADDED)) {
/* 162 */         boms.add(row);
/*     */       }
/*     */     } 
/* 165 */     return boms;
/*     */   }
/*     */ }


/* Location:              {{SOURCE_PATH}}\com\jiexin\pl\\util\AgileUtils.class
 * Java compiler version: 8 (52.0)
 * JD-Core Version:       1.1.3
 */