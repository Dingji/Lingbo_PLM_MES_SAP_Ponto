/*      */ package com.jiexin.plm.util;
/*      */ import com.agile.api.APIException;
/*      */ import com.agile.api.IAgileClass;
/*      */ import com.agile.api.IAgileList;
/*      */ import com.agile.api.IAgileSession;
/*      */ import com.agile.api.IAttribute;
/*      */ import com.agile.api.ICell;
/*      */ import com.agile.api.IChange;
/*      */ import com.agile.api.IDataObject;
/*      */ import com.agile.api.IItem;
/*      */ import com.agile.api.IProperty;
/*      */ import com.agile.api.IQuery;
/*      */ import com.agile.api.IRow;
/*      */ import com.agile.api.IStatus;
/*      */ import com.agile.api.ITable;
/*      */ import com.agile.api.ITableDesc;
/*      */ import com.agile.api.ITwoWayIterator;
/*      */ import com.agile.api.ItemConstants;
/*      */ import com.agile.api.PropertyConstants;
/*      */ import com.agile.api.TableTypeConstants;
/*      */ import com.agile.px.IUpdateEventInfo;
/*      */ import java.util.ArrayList;
/*      */ import java.util.HashMap;
/*      */ import java.util.Iterator;
/*      */ import java.util.List;
/*      */ import java.util.Map;
/*      */ 
/*      */ public class AgileUtil {
/*      */   public static IItem getItemVersion(IAgileSession session, IItem item, String itemVersion) throws APIException {
/*   30 */     IItem referent = null;
/*   31 */     Map revisions = item.getRevisions();
/*      */     
/*   33 */     ITable tableChange = item.getTable(ItemConstants.TABLE_CHANGEHISTORY);
/*   34 */     System.out.println("变更单数量：" + tableChange.size());
/*   35 */     if (tableChange.size() == 0) {
/*   36 */       String versionNew = "(" + itemVersion + ")";
/*   37 */       String rowNumber = "";
/*   38 */       boolean b = hasPendingChange(item);
/*   39 */       if (b == true) {
/*   40 */         ITable table = item.getTable(ItemConstants.TABLE_PENDINGCHANGES);
/*   41 */         if (table.size() == 1) {
/*   42 */           for (Object pendChange : table) {
/*   43 */             IRow pendChangeRow = (IRow)pendChange;
/*   44 */             String rowVersion = pendChangeRow.getValue(Integer.valueOf(1149)).toString();
/*   45 */             if (versionNew.equals(rowVersion)) {
/*   46 */               rowNumber = pendChangeRow.getValue(Integer.valueOf(1026)).toString();
/*      */             }
/*      */           } 
/*      */         }
/*      */       } 
/*   51 */       if (!"".equals(rowNumber)) {
/*   52 */         IChange change = (IChange)session.getObject(3, rowNumber);
/*   53 */         ITable table = change.getTable(ChangeConstants.TABLE_AFFECTEDITEMS);
/*   54 */         for (Object o1 : table) {
/*   55 */           IRow iRow = (IRow)o1;
/*   56 */           String iRowNumber = iRow.getValue(Integer.valueOf(1054)).toString();
/*   57 */           if (item.getName().equals(iRowNumber)) {
/*   58 */             referent = (IItem)iRow.getReferent();
/*      */           }
/*      */         } 
/*      */       } 
/*      */     } else {
/*   63 */       for (Object o : tableChange) {
/*   64 */         IRow row = (IRow)o;
/*   65 */         IChange changeOld = (IChange)row.getReferent();
/*   66 */         System.out.println("变更单编号：" + changeOld.getName());
/*   67 */         String version = (String)revisions.get(changeOld);
/*   68 */         System.out.println("version = " + version);
/*   69 */         if (itemVersion.equals(version)) {
/*      */           
/*   71 */           System.out.println("item   " + item + "   的版本是  " + version);
/*   72 */           ITable table = changeOld.getTable(ChangeConstants.TABLE_AFFECTEDITEMS);
/*   73 */           for (Object o1 : table) {
/*   74 */             IRow iRow = (IRow)o1;
/*   75 */             String iRowNumber = iRow.getValue(Integer.valueOf(1054)).toString();
/*   76 */             if (item.getName().equals(iRowNumber))
/*   77 */               referent = (IItem)iRow.getReferent(); 
/*      */           } 
/*      */           continue;
/*      */         } 
/*   81 */         String versionNew = "(" + itemVersion + ")";
/*   82 */         String rowNumber = "";
/*   83 */         boolean b = hasPendingChange(item);
/*   84 */         if (b == true) {
/*   85 */           ITable table = item.getTable(ItemConstants.TABLE_PENDINGCHANGES);
/*   86 */           if (table.size() == 1) {
/*   87 */             for (Object pendChange : table) {
/*   88 */               IRow pendChangeRow = (IRow)pendChange;
/*   89 */               String rowVersion = pendChangeRow.getValue(Integer.valueOf(1149)).toString();
/*   90 */               if (versionNew.equals(rowVersion)) {
/*   91 */                 rowNumber = pendChangeRow.getValue(Integer.valueOf(1026)).toString();
/*      */               }
/*      */             } 
/*      */           }
/*      */         } 
/*   96 */         if (!"".equals(rowNumber)) {
/*   97 */           IChange change = (IChange)session.getObject(3, rowNumber);
/*   98 */           ITable table = change.getTable(ChangeConstants.TABLE_AFFECTEDITEMS);
/*   99 */           for (Object o1 : table) {
/*  100 */             IRow iRow = (IRow)o1;
/*  101 */             String iRowNumber = iRow.getValue(Integer.valueOf(1054)).toString();
/*  102 */             if (item.getName().equals(iRowNumber)) {
/*  103 */               referent = (IItem)iRow.getReferent();
/*      */             }
/*      */           } 
/*      */         } 
/*      */       } 
/*      */     } 
/*      */ 
/*      */     
/*  111 */     return referent;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static void setCellValues(ICell cell, List<IItem> list) throws APIException {
/*  120 */     Object[] docs = new Object[list.size()];
/*  121 */     for (int i = 0; i < list.size(); i++) {
/*  122 */       docs[i] = list.get(i);
/*      */     }
/*  124 */     IAgileList availableValues = cell.getAvailableValues();
/*  125 */     availableValues.setSelection(docs);
/*  126 */     cell.setValue(availableValues);
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static SqlSession sqlSessionFactoryDescription() throws IOException {
/*  137 */     InputStream inputStream = Resources.getResourceAsStream("mybatis-config.xml");
/*      */     
/*  139 */     SqlSessionFactory factory = (new SqlSessionFactoryBuilder()).build(inputStream);
/*      */     
/*  141 */     return factory.openSession();
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static String getNextNumber(IAgileSession session, String itemType) throws APIException {
/*  153 */     IAutoNumber[] array = session.getAdminInstance().getAgileClass(itemType).getAutoNumberSources();
/*  154 */     String changeNumber = array[0].getNextNumber();
/*  155 */     return changeNumber;
/*      */   }
/*      */ 
/*      */   
/*      */   public static boolean getYn(IUpdateTitleBlockEventInfo iUpdateTitleBlockEventInfo, int strNumber) throws Exception {
/*  160 */     boolean yn = false;
/*      */     
/*  162 */     Integer[] attributeIds = iUpdateTitleBlockEventInfo.getAttributeIds();
/*  163 */     for (Integer attributeId : attributeIds) {
/*  164 */       if (attributeId.intValue() == strNumber) {
/*  165 */         yn = true;
/*      */       }
/*      */     } 
/*  168 */     return yn;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static String getValue(IDataObject obj, Integer id) throws APIException {
/*  180 */     String result = "";
/*  181 */     Object tempObj = null;
/*  182 */     tempObj = (obj.getCell(id) == null || obj.getValue(id) == null) ? "" : obj.getValue(id);
/*  183 */     if (tempObj instanceof java.sql.Date) {
/*  184 */       SimpleDateFormat sdf = new SimpleDateFormat("YYYY/MM/dd HH:mm:ss");
/*  185 */       result = sdf.format(tempObj);
/*      */     } else {
/*  187 */       result = tempObj.toString();
/*      */     } 
/*  189 */     return result;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static ArrayList GetMaximumRev(IItem item) throws APIException {
/*  200 */     ArrayList<String> list = new ArrayList();
/*  201 */     Map revisions = item.getRevisions();
/*  202 */     for (Object rev : revisions.values()) {
/*  203 */       String revison = rev.toString();
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */       
/*  212 */       if (!StringUtil.isNum(revison)) {
/*      */         continue;
/*      */       }
/*  215 */       list.add(revison);
/*      */     } 
/*  217 */     return list;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static IUser getUser(IAgileSession session, String loginId) throws APIException {
/*  226 */     return (IUser)session.getObject(42, loginId);
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static IChange getNewestChange(IAgileSession session, String itemNo) throws APIException {
/*  235 */     IItem item = (IItem)session.getObject(2, itemNo);
/*  236 */     ITable table = item.getTable(ItemConstants.TABLE_PENDINGCHANGES);
/*  237 */     Iterator<IRow> iterator = table.iterator();
/*  238 */     IChange change = null;
/*  239 */     if (iterator.hasNext()) {
/*  240 */       IRow row = iterator.next();
/*  241 */       change = (IChange)row.getReferent();
/*      */     } 
/*  243 */     return change;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static String getValue(IItem obj, Integer id, IChange change) throws APIException {
/*  258 */     String result = "";
/*  259 */     IAgileSession session = obj.getSession();
/*  260 */     String itemType = obj.getValue(ItemConstants.ATT_TITLE_BLOCK_ITEM_TYPE).toString();
/*      */     
/*  262 */     if (change != null && isInChange(obj, change) && 
/*  263 */       isChangeControl(session, id, itemType)) {
/*  264 */       result = getRedlineValue(obj, id, change);
/*      */     } else {
/*  266 */       result = getValue((IDataObject)obj, id);
/*      */     } 
/*  268 */     return result;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static boolean isInitialItem(IItem item) throws APIException {
/*  279 */     boolean flag = false;
/*  280 */     if (!item.isFlagSet(ItemConstants.FLAG_HAS_RELEASED_REV)) {
/*  281 */       flag = true;
/*      */     }
/*  283 */     return flag;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static boolean isInPendingChange(IItem item) throws APIException {
/*  294 */     boolean flag = false;
/*  295 */     String rev = item.getRevision();
/*  296 */     if (rev != null && !rev.equals("") && rev.contains("(") && rev.contains(")")) {
/*  297 */       flag = true;
/*      */     }
/*  299 */     return flag;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static boolean isInChange(IItem item, IChange change) throws APIException {
/*  310 */     boolean isInChange = false;
/*  311 */     if (hasPendingChange(item)) {
/*  312 */       ITable pendingEco = item.getTable(ItemConstants.TABLE_PENDINGCHANGES);
/*  313 */       ITwoWayIterator pendingEcoIter = pendingEco.getTableIterator();
/*  314 */       while (pendingEcoIter.hasNext()) {
/*  315 */         IRow row = (IRow)pendingEcoIter.next();
/*  316 */         IChange tempChange = (IChange)row.getReferent();
/*  317 */         if (change.getName().equals(tempChange.getName())) {
/*  318 */           isInChange = true;
/*      */           break;
/*      */         } 
/*      */       } 
/*      */     } 
/*  323 */     return isInChange;
/*      */   }
/*      */ 
/*      */   
/*      */   public static boolean isHasBom(IItem item) throws APIException {
/*  328 */     boolean flag = false;
/*  329 */     ITable table = item.getTable(ItemConstants.TABLE_BOM);
/*  330 */     Iterator iterator = table.iterator();
/*  331 */     if (iterator.hasNext()) {
/*  332 */       flag = true;
/*      */     }
/*  334 */     return flag;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static boolean isInOther(IItem item, IChange change) throws APIException {
/*  345 */     boolean isInChange = false;
/*  346 */     if (hasPendingChange(item)) {
/*  347 */       ITable pendingEco = item.getTable(ItemConstants.TABLE_PENDINGCHANGES);
/*  348 */       ITwoWayIterator pendingEcoIter = pendingEco.getTableIterator();
/*  349 */       while (pendingEcoIter.hasNext()) {
/*  350 */         IRow row = (IRow)pendingEcoIter.next();
/*  351 */         IChange tempChange = (IChange)row.getReferent();
/*  352 */         if (change.getName().equals(tempChange.getName())) {
/*  353 */           isInChange = true;
/*      */           break;
/*      */         } 
/*      */       } 
/*      */     } 
/*  358 */     return isInChange;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static boolean hasPendingChange(IItem item) throws APIException {
/*  369 */     boolean flag = false;
/*  370 */     if (item.isFlagSet(ItemConstants.FLAG_HAS_PENDING_ECO)) {
/*  371 */       flag = true;
/*      */     }
/*  373 */     return flag;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static IChange getPendingChange(IItem item) throws APIException {
/*  384 */     IChange change = null;
/*  385 */     if (isInPendingChange(item)) {
/*  386 */       change = item.getChange();
/*      */     } else {
/*  388 */       ITable pendingEco = item.getTable(ItemConstants.TABLE_PENDINGCHANGES);
/*  389 */       ITwoWayIterator pendingEcoIter = pendingEco.getTableIterator();
/*  390 */       if (pendingEcoIter.hasNext()) {
/*  391 */         IRow row = (IRow)pendingEcoIter.next();
/*  392 */         change = (IChange)row.getReferent();
/*      */       } 
/*      */     } 
/*  395 */     return change;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static List<IItem> getUnReleasedChildrenRedline(IItem parent) throws APIException {
/*  406 */     List<IItem> unReleasedChildren = new ArrayList<>();
/*  407 */     ITable table = parent.getTable(ItemConstants.TABLE_REDLINEBOM);
/*  408 */     Iterator<IRow> childrenRow = table.iterator();
/*  409 */     while (childrenRow.hasNext()) {
/*  410 */       IRow row = childrenRow.next();
/*      */       
/*  412 */       Boolean statusRemovedBOM = Boolean.valueOf(row.isFlagSet(ItemConstants.FLAG_IS_REDLINE_REMOVED));
/*  413 */       if (statusRemovedBOM.booleanValue()) {
/*      */         continue;
/*      */       }
/*  416 */       IItem child = (IItem)row.getReferent();
/*      */       
/*  418 */       List<IItem> tempList = getUnReleasedChildren(child);
/*  419 */       if (tempList != null && tempList.size() > 0) {
/*  420 */         unReleasedChildren = addItemToList(unReleasedChildren, tempList);
/*      */       }
/*      */     } 
/*  423 */     return unReleasedChildren;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static List<IItem> getUnReleasedChildren(IItem parent) throws APIException {
/*  434 */     List<IItem> unReleasedChildren = new ArrayList<>();
/*  435 */     ITable table = parent.getTable(ItemConstants.TABLE_BOM);
/*  436 */     Iterator<IRow> childrenRow = table.iterator();
/*  437 */     while (childrenRow.hasNext()) {
/*  438 */       IRow row = childrenRow.next();
/*  439 */       IItem child = (IItem)row.getReferent();
/*  440 */       List<IItem> tempList = getUnReleasedChildren(child);
/*  441 */       if (tempList != null && tempList.size() > 0) {
/*  442 */         unReleasedChildren = addItemToList(unReleasedChildren, tempList);
/*      */       }
/*      */     } 
/*      */     
/*  446 */     if (isInitialItem(parent)) {
/*  447 */       unReleasedChildren = addItemToList(unReleasedChildren, parent);
/*      */     }
/*  449 */     return unReleasedChildren;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static List<IItem> getAllParents(IItem child) throws APIException {
/*  459 */     List<IItem> allParents = new ArrayList<>();
/*  460 */     ITable table = child.getTable(ItemConstants.TABLE_WHEREUSED);
/*  461 */     Iterator<IRow> parentsRow = table.iterator();
/*  462 */     while (parentsRow.hasNext()) {
/*  463 */       IRow row = parentsRow.next();
/*  464 */       IItem p = (IItem)row.getReferent();
/*  465 */       List<IItem> tempList = getAllParents(p);
/*  466 */       if (tempList != null && tempList.size() > 0) {
/*  467 */         allParents = addItemToList(allParents, tempList);
/*      */       }
/*  469 */       allParents = addItemToList(allParents, p);
/*      */     } 
/*  471 */     return allParents;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static List<IItem> addItemToList(List<IItem> list, IItem item) throws APIException {
/*  482 */     boolean isExist = false;
/*  483 */     for (IItem temp : list) {
/*  484 */       if (temp.getName().equals(item.getName())) {
/*  485 */         isExist = true;
/*      */         break;
/*      */       } 
/*      */     } 
/*  489 */     if (!isExist) {
/*  490 */       list.add(item);
/*      */     }
/*      */     
/*  493 */     return list;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static List<IItem> addItemToList(List<IItem> list, List<IItem> toAddItems) throws APIException {
/*  503 */     for (IItem toAddItem : toAddItems) {
/*  504 */       boolean isExist = false;
/*  505 */       for (IItem temp : list) {
/*  506 */         if (temp.getName().equals(toAddItem.getName())) {
/*  507 */           isExist = true;
/*      */           break;
/*      */         } 
/*      */       } 
/*  511 */       if (!isExist) {
/*  512 */         list.add(toAddItem);
/*      */       }
/*      */     } 
/*      */     
/*  516 */     return list;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static List<IItem> getAllChildren(IItem parent) throws APIException {
/*  527 */     List<IItem> allChildren = new ArrayList<>();
/*  528 */     ITable table = parent.getTable(ItemConstants.TABLE_BOM);
/*  529 */     Iterator<IRow> childrenRow = table.iterator();
/*  530 */     while (childrenRow.hasNext()) {
/*  531 */       IRow row = childrenRow.next();
/*  532 */       IItem child = (IItem)row.getReferent();
/*  533 */       List<IItem> tempList = getAllChildren(child);
/*  534 */       if (tempList != null && tempList.size() > 0) {
/*  535 */         allChildren = addItemToList(allChildren, tempList);
/*      */       }
/*      */     } 
/*  538 */     allChildren = addItemToList(allChildren, parent);
/*  539 */     return allChildren;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static void getPrice(IItem parent) throws APIException {
/*  550 */     ITable bom = parent.getTable(ItemConstants.TABLE_BOM);
/*  551 */     ITwoWayIterator bomIt = bom.getTableIterator();
/*      */     
/*  553 */     double total = 0.0D;
/*      */     
/*  555 */     boolean flg = false;
/*  556 */     while (bomIt.hasNext()) {
/*  557 */       IRow row = (IRow)bomIt.next();
/*  558 */       IItem child = (IItem)row.getReferent();
/*  559 */       if (!child.getAgileClass().getSuperClass().getAPIName().equals("PartsClass")) {
/*      */         continue;
/*      */       }
/*  562 */       getPrice(child);
/*  563 */       flg = true;
/*      */ 
/*      */       
/*  566 */       String number = (row.getCell(ItemConstants.ATT_BOM_QTY) == null) ? "" : ((row.getCell(ItemConstants.ATT_BOM_QTY).getValue().toString() == "") ? "0" : row.getCell(ItemConstants.ATT_BOM_QTY).getValue().toString());
/*      */ 
/*      */       
/*  569 */       String per = (child.getCell(ItemConstants.ATT_PAGE_TWO_TEXT07) == null) ? "" : ((child.getCell(ItemConstants.ATT_PAGE_TWO_TEXT07).getValue().toString() == "") ? "0" : child.getCell(ItemConstants.ATT_PAGE_TWO_TEXT07).getValue().toString());
/*  570 */       double price = (StringUtil.converToFloat(number).floatValue() * StringUtil.converToFloat(per).floatValue());
/*  571 */       total += price;
/*      */     } 
/*  573 */     if (flg)
/*      */     {
/*  575 */       parent.setValue(ItemConstants.ATT_PAGE_TWO_TEXT07, String.valueOf(total));
/*      */     }
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static String getValue(IItem item, IUpdateEventInfo objectEventInfo, Integer id, String str) throws Exception {
/*  587 */     String value = "";
/*  588 */     if (item == null) {
/*      */ 
/*      */ 
/*      */ 
/*      */       
/*  593 */       value = (objectEventInfo.getCell(id) == null) ? "" : ((objectEventInfo.getCell(id).getValue() == null) ? "" : ((objectEventInfo.getCell(id).getValue().toString() == "") ? "" : (objectEventInfo.getCell(id).getValue().toString().trim() + str)));
/*      */     
/*      */     }
/*      */     else {
/*      */ 
/*      */       
/*  599 */       value = (item.getCell(id) == null) ? "" : ((item.getCell(id).getValue() == null) ? "" : ((item.getCell(id).getValue().toString() == "") ? "" : (item.getCell(id).getValue().toString().trim() + str)));
/*      */     } 
/*  601 */     return value;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static String getValue(IItem item, Integer id, String str) throws Exception {
/*  611 */     String value = "";
/*      */ 
/*      */ 
/*      */ 
/*      */     
/*  616 */     value = (item.getCell(id) == null) ? "" : ((item.getCell(id).getValue() == null) ? "" : ((item.getCell(id).getValue().toString() == "") ? "" : (item.getCell(id).getValue().toString().trim() + str)));
/*      */     
/*  618 */     return value;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static String getRedlineValue(IItem item, Integer attrId) throws APIException {
/*  630 */     IChange change = getPendingChange(item);
/*  631 */     String value = getRedlineValue(item, attrId, change);
/*  632 */     return value;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static String getRedlineValue(IItem item, Integer attrId, IChange change, boolean hasSupplier) throws APIException {
/*  643 */     Map<Object, Object> map = new HashMap<>();
/*  644 */     String value = "";
/*  645 */     if (change != null) {
/*  646 */       item.setRevision(change);
/*      */       
/*  648 */       ITable tbTable = item.getTable(ItemConstants.TABLE_REDLINETITLEBLOCK);
/*  649 */       ITwoWayIterator tbIter = tbTable.getTableIterator();
/*  650 */       if (tbIter.hasNext()) {
/*  651 */         IRow tempRow = (IRow)tbIter.next();
/*  652 */         ICell[] tempCells = tempRow.getCells();
/*  653 */         for (ICell tempCell : tempCells) {
/*  654 */           map.put(tempCell.getId(), tempCell.getValue());
/*      */         }
/*      */       } 
/*      */       
/*  658 */       ITable p2Table = item.getTable(ItemConstants.TABLE_REDLINEPAGETWO);
/*  659 */       ITwoWayIterator p2Iter = p2Table.getTableIterator();
/*  660 */       if (p2Iter.hasNext()) {
/*  661 */         IRow tempRow = (IRow)p2Iter.next();
/*  662 */         ICell[] tempCells = tempRow.getCells();
/*  663 */         for (ICell tempCell : tempCells) {
/*  664 */           map.put(tempCell.getId(), tempCell.getValue());
/*      */         }
/*      */       } 
/*      */       
/*  668 */       ITable p3Table = item.getTable(ItemConstants.TABLE_REDLINEPAGETHREE);
/*  669 */       ITwoWayIterator p3Iter = p3Table.getTableIterator();
/*  670 */       if (p3Iter.hasNext()) {
/*  671 */         IRow tempRow = (IRow)p3Iter.next();
/*  672 */         ICell[] tempCells = tempRow.getCells();
/*  673 */         for (ICell tempCell : tempCells) {
/*  674 */           map.put(tempCell.getId(), tempCell.getValue());
/*      */         }
/*      */       } 
/*  677 */       if (hasSupplier) {
/*  678 */         ITable supplierTable = item.getTable(ItemConstants.TABLE_SUPPLIERS);
/*  679 */         ITwoWayIterator supplierIter = supplierTable.getTableIterator();
/*  680 */         if (supplierIter.hasNext()) {
/*  681 */           IRow tempRow = (IRow)supplierIter.next();
/*  682 */           ICell[] tempCells = tempRow.getCells();
/*  683 */           for (ICell tempCell : tempCells) {
/*  684 */             map.put(tempCell.getId(), tempCell.getValue());
/*      */           }
/*      */         } 
/*      */       } 
/*  688 */       value = (map.get(attrId) == null) ? "" : map.get(attrId).toString();
/*      */     } 
/*  690 */     return value;
/*      */   }
/*      */   
/*      */   public static String getRedlineValueByItem(IItem item, Integer attrId) throws APIException {
/*  694 */     Map<Object, Object> map = new HashMap<>();
/*  695 */     String value = "";
/*      */     
/*  697 */     ITable tbTable = item.getTable(ItemConstants.TABLE_REDLINETITLEBLOCK);
/*  698 */     ITwoWayIterator tbIter = tbTable.getTableIterator();
/*  699 */     if (tbIter.hasNext()) {
/*  700 */       IRow tempRow = (IRow)tbIter.next();
/*  701 */       ICell[] tempCells = tempRow.getCells();
/*  702 */       for (ICell tempCell : tempCells) {
/*  703 */         map.put(tempCell.getId(), tempCell.getValue());
/*      */       }
/*      */     } 
/*      */     
/*  707 */     ITable p2Table = item.getTable(ItemConstants.TABLE_REDLINEPAGETWO);
/*  708 */     ITwoWayIterator p2Iter = p2Table.getTableIterator();
/*  709 */     if (p2Iter.hasNext()) {
/*  710 */       IRow tempRow = (IRow)p2Iter.next();
/*  711 */       ICell[] tempCells = tempRow.getCells();
/*  712 */       for (ICell tempCell : tempCells) {
/*  713 */         map.put(tempCell.getId(), tempCell.getValue());
/*      */       }
/*      */     } 
/*      */     
/*  717 */     ITable p3Table = item.getTable(ItemConstants.TABLE_REDLINEPAGETHREE);
/*  718 */     ITwoWayIterator p3Iter = p3Table.getTableIterator();
/*  719 */     if (p3Iter.hasNext()) {
/*  720 */       IRow tempRow = (IRow)p3Iter.next();
/*  721 */       ICell[] tempCells = tempRow.getCells();
/*  722 */       for (ICell tempCell : tempCells) {
/*  723 */         map.put(tempCell.getId(), tempCell.getValue());
/*      */       }
/*      */     } 
/*  726 */     value = (map.get(attrId) == null) ? "" : map.get(attrId).toString();
/*  727 */     return value;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static String getRedlineValue(IItem item, Integer attrId, IChange change) throws APIException {
/*  737 */     Map<Object, Object> map = new HashMap<>();
/*  738 */     String value = "";
/*  739 */     if (change != null) {
/*  740 */       item.setRevision(change);
/*      */       
/*  742 */       ITable tbTable = item.getTable(ItemConstants.TABLE_REDLINETITLEBLOCK);
/*  743 */       ITwoWayIterator tbIter = tbTable.getTableIterator();
/*  744 */       if (tbIter.hasNext()) {
/*  745 */         IRow tempRow = (IRow)tbIter.next();
/*  746 */         ICell[] tempCells = tempRow.getCells();
/*  747 */         for (ICell tempCell : tempCells) {
/*  748 */           map.put(tempCell.getId(), tempCell.getValue());
/*      */         }
/*      */       } 
/*      */       
/*  752 */       ITable p2Table = item.getTable(ItemConstants.TABLE_REDLINEPAGETWO);
/*  753 */       ITwoWayIterator p2Iter = p2Table.getTableIterator();
/*  754 */       if (p2Iter.hasNext()) {
/*  755 */         IRow tempRow = (IRow)p2Iter.next();
/*  756 */         ICell[] tempCells = tempRow.getCells();
/*  757 */         for (ICell tempCell : tempCells) {
/*  758 */           map.put(tempCell.getId(), tempCell.getValue());
/*      */         }
/*      */       } 
/*      */       
/*  762 */       ITable p3Table = item.getTable(ItemConstants.TABLE_REDLINEPAGETHREE);
/*  763 */       ITwoWayIterator p3Iter = p3Table.getTableIterator();
/*  764 */       if (p3Iter.hasNext()) {
/*  765 */         IRow tempRow = (IRow)p3Iter.next();
/*  766 */         ICell[] tempCells = tempRow.getCells();
/*  767 */         for (ICell tempCell : tempCells) {
/*  768 */           map.put(tempCell.getId(), tempCell.getValue());
/*      */         }
/*      */       } 
/*  771 */       value = (map.get(attrId) == null) ? "" : map.get(attrId).toString();
/*      */     } 
/*  773 */     return value;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static String getRedlineListApi(IItem item, Integer attrId) throws APIException {
/*  789 */     Map<Object, Object> map = new HashMap<>();
/*  790 */     String value = "";
/*      */     
/*  792 */     ITable tbTable = item.getTable(ItemConstants.TABLE_REDLINETITLEBLOCK);
/*  793 */     ITwoWayIterator tbIter = tbTable.getTableIterator();
/*  794 */     if (tbIter.hasNext()) {
/*  795 */       IRow tempRow = (IRow)tbIter.next();
/*  796 */       ICell[] tempCells = tempRow.getCells();
/*  797 */       for (ICell tempCell : tempCells) {
/*  798 */         map.put(tempCell.getId(), tempCell.getValue());
/*      */       }
/*      */     } 
/*      */     
/*  802 */     ITable p2Table = item.getTable(ItemConstants.TABLE_REDLINEPAGETWO);
/*  803 */     ITwoWayIterator p2Iter = p2Table.getTableIterator();
/*  804 */     if (p2Iter.hasNext()) {
/*  805 */       IRow tempRow = (IRow)p2Iter.next();
/*  806 */       ICell[] tempCells = tempRow.getCells();
/*  807 */       for (ICell tempCell : tempCells) {
/*  808 */         map.put(tempCell.getId(), tempCell.getValue());
/*      */       }
/*      */     } 
/*      */     
/*  812 */     ITable p3Table = item.getTable(ItemConstants.TABLE_REDLINEPAGETHREE);
/*  813 */     ITwoWayIterator p3Iter = p3Table.getTableIterator();
/*  814 */     if (p3Iter.hasNext()) {
/*  815 */       IRow tempRow = (IRow)p3Iter.next();
/*  816 */       ICell[] tempCells = tempRow.getCells();
/*  817 */       for (ICell tempCell : tempCells) {
/*  818 */         map.put(tempCell.getId(), tempCell.getValue());
/*      */       }
/*      */     } 
/*  821 */     if (map.get(attrId) != null) {
/*  822 */       IAgileList agileList = (IAgileList)map.get(attrId);
/*  823 */       IAgileList[] arrayOfIAgileList = agileList.getSelection(); int i = arrayOfIAgileList.length; byte b = 0; if (b < i) { IAgileList iAgileList = arrayOfIAgileList[b];
/*  824 */         return iAgileList.getAPIName(); }
/*      */     
/*      */     } 
/*  827 */     return value;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static String getCurrentStatusName(IChange change) throws APIException {
/*  838 */     String currentStatusName = "";
/*  839 */     IStatus currentStatus = change.getStatus();
/*  840 */     if (currentStatus != null) {
/*  841 */       currentStatusName = currentStatus.getName();
/*      */     }
/*  843 */     return currentStatusName;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static String getCurrentStatusAPIName(IChange change) throws APIException {
/*  854 */     String currentStatusName = "";
/*  855 */     IStatus currentStatus = change.getStatus();
/*  856 */     if (currentStatus != null) {
/*  857 */       currentStatusName = currentStatus.getAPIName();
/*      */     }
/*  859 */     return currentStatusName;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static String getNextStatusName(IChange change) throws APIException {
/*  870 */     String nextStatusName = "";
/*  871 */     IStatus nextStatus = change.getDefaultNextStatus();
/*  872 */     if (nextStatus != null) {
/*  873 */       nextStatusName = nextStatus.getName();
/*      */     }
/*  875 */     return nextStatusName;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static void setRedlineValue(IItem item, Integer tableId, Integer attributeId, String value) throws APIException {
/*  908 */     ITable pRedline = item.getTable(tableId);
/*  909 */     ITwoWayIterator it = pRedline.getTableIterator();
/*  910 */     if (it.hasNext()) {
/*  911 */       IRow tempRow = (IRow)it.next();
/*  912 */       ICell tempCell = tempRow.getCell(attributeId);
/*  913 */       tempCell.setValue(value);
/*      */     } 
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static boolean isUnique(IAgileSession session, String itemNo, String value) throws APIException {
/*  927 */     boolean result = true;
/*  928 */     IQuery query = (IQuery)session.createObject(-5, ItemConstants.CLASS_PARTS_CLASS);
/*      */     
/*  930 */     query.setCaseSensitive(false);
/*  931 */     String condition = "[1002] equal to %0 and [1001] not equal to %1 and [1084] not equal to %2";
/*      */     
/*  933 */     query.setCriteria(condition);
/*  934 */     String lifeStyle = "报废";
/*  935 */     ITable results = query.execute(new Object[] { value, itemNo, lifeStyle });
/*  936 */     if (results.size() >= 1)
/*  937 */       result = false; 
/*  938 */     return result;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static boolean isUnique(IAgileSession session, String itemNo, Integer attrId, String value) throws APIException {
/*  953 */     boolean result = true;
/*  954 */     IQuery query = (IQuery)session.createObject(-5, ItemConstants.CLASS_PARTS_CLASS);
/*      */     
/*  956 */     query.setCaseSensitive(false);
/*  957 */     String condition = "[" + attrId + "] equal to %0 and [1001] not equal to %1";
/*  958 */     query.setCriteria(condition);
/*  959 */     ITable results = query.execute(new Object[] { value, itemNo });
/*  960 */     Iterator<IRow> iterator = results.iterator();
/*  961 */     while (iterator.hasNext()) {
/*  962 */       IRow row = iterator.next();
/*  963 */       IItem iItem = (IItem)row.getReferent();
/*      */     } 
/*      */     
/*  966 */     if (results.size() >= 1)
/*  967 */       result = false; 
/*  968 */     return result;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static boolean erpDescIsUnique(IAgileSession session, String itemNo, String value) throws APIException {
/*  983 */     boolean result = true;
/*  984 */     IQuery query = (IQuery)session.createObject(-5, ItemConstants.CLASS_PARTS_CLASS);
/*      */     
/*  986 */     query.setCaseSensitive(false);
/*  987 */     String condition = "[1335] equal to %0 and [1001] not equal to %1 and [1084] not equal to %2";
/*      */     
/*  989 */     query.setCriteria(condition);
/*  990 */     String lifeStyle = "报废";
/*  991 */     ITable results = query.execute(new Object[] { value, itemNo, lifeStyle });
/*  992 */     if (results.size() >= 1)
/*  993 */       result = false; 
/*  994 */     return result;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static boolean oldNameisUnique(IAgileSession session, String itemNo, String value) throws APIException {
/* 1009 */     boolean result = true;
/* 1010 */     IQuery query = (IQuery)session.createObject(-5, ItemConstants.CLASS_PARTS_CLASS);
/*      */     
/* 1012 */     query.setCaseSensitive(false);
/* 1013 */     String condition = "[2020] not equal to %0 and [1002] equal to %1 and [1001] not equal to %2 and [1084] not equal to %3";
/*      */     
/* 1015 */     query.setCriteria(condition);
/* 1016 */     String name = "注塑";
/* 1017 */     String lifeStyle = "报废";
/* 1018 */     ITable results = query.execute(new Object[] { name, value, itemNo, lifeStyle });
/* 1019 */     if (results.size() >= 1)
/* 1020 */       result = false; 
/* 1021 */     return result;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static boolean oldNameisUniqueIsZS(IAgileSession session, String itemNo, String value) throws APIException {
/* 1035 */     boolean result = true;
/* 1036 */     IQuery query = (IQuery)session.createObject(-5, ItemConstants.CLASS_PARTS_CLASS);
/*      */     
/* 1038 */     query.setCaseSensitive(false);
/* 1039 */     String condition = "[2020] equal to %0 and [1002] equal to %1 and [1001] not equal to %2 and [1084] not equal to %3";
/*      */     
/* 1041 */     query.setCriteria(condition);
/* 1042 */     String name = "注塑";
/* 1043 */     String lifeStyle = "报废";
/* 1044 */     ITable results = query.execute(new Object[] { name, value, itemNo, lifeStyle });
/* 1045 */     if (results.size() >= 1)
/* 1046 */       result = false; 
/* 1047 */     return result;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   private static boolean isRequired(IAttribute attr) throws APIException {
/* 1058 */     boolean result = false;
/* 1059 */     IProperty required = attr.getProperty(PropertyConstants.PROP_REQUIRED);
/* 1060 */     if (required != null) {
/* 1061 */       Object value = required.getValue();
/* 1062 */       if (value != null) {
/* 1063 */         String tempVal = value.toString();
/* 1064 */         result = (tempVal.equals("是") || tempVal.equalsIgnoreCase("TRUE"));
/*      */       } 
/*      */     } 
/* 1067 */     return result;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   private static boolean isVisible(IAttribute attr) throws APIException {
/* 1078 */     boolean result = false;
/* 1079 */     IProperty visible = attr.getProperty(PropertyConstants.PROP_VISIBLE);
/* 1080 */     if (visible != null) {
/* 1081 */       Object value = visible.getValue();
/* 1082 */       if (value != null) {
/* 1083 */         String tempVal = value.toString();
/* 1084 */         result = (tempVal.equals("是") || tempVal.equalsIgnoreCase("TRUE"));
/*      */       } 
/*      */     } 
/* 1087 */     return result;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static boolean isChangeControl(IAttribute attr) throws APIException {
/* 1098 */     boolean result = false;
/* 1099 */     if (attr != null) {
/* 1100 */       IProperty changeControlled = attr.getProperty(PropertyConstants.PROP_CHANGE_CONTROLLED);
/* 1101 */       if (changeControlled != null) {
/* 1102 */         Object value = changeControlled.getValue();
/* 1103 */         if (value != null) {
/* 1104 */           String tempVal = value.toString();
/* 1105 */           result = (tempVal.equals("是") || tempVal.equalsIgnoreCase("TRUE"));
/*      */         } 
/*      */       } 
/*      */     } 
/* 1109 */     return result;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static boolean isChangeControl(IAgileSession session, Integer id, String itemType) throws APIException {
/* 1122 */     boolean result = false;
/* 1123 */     Map<Integer, IAttribute> attrs = getVisibleAttributes(session, itemType);
/* 1124 */     IAttribute attr = attrs.get(id);
/* 1125 */     if (attr != null) {
/* 1126 */       result = isChangeControl(attr);
/*      */     }
/* 1128 */     return result;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static Map<Integer, IAttribute> getVisibleAttributes(IAgileSession session, String itemType) throws APIException {
/* 1141 */     Map<Integer, IAttribute> result = new HashMap<>();
/* 1142 */     IAgileClass cls = session.getAdminInstance().getAgileClass(itemType);
/*      */ 
/*      */     
/* 1145 */     if (cls != null && 
/* 1146 */       !cls.isAbstract()) {
/* 1147 */       IAttribute[] attrs = null;
/*      */ 
/*      */       
/* 1150 */       ITableDesc page1 = cls.getTableDescriptor(TableTypeConstants.TYPE_PAGE_ONE);
/* 1151 */       if (page1 != null) {
/* 1152 */         attrs = page1.getAttributes();
/* 1153 */         for (int i = 0; i < attrs.length; i++) {
/* 1154 */           IAttribute attr = attrs[i];
/* 1155 */           if (isVisible(attr)) {
/* 1156 */             result.put(
/* 1157 */                 Integer.valueOf(Integer.parseInt(attr.getId().toString())), attr);
/*      */           }
/*      */         } 
/*      */       } 
/*      */ 
/*      */ 
/*      */       
/* 1164 */       ITableDesc page2 = cls.getTableDescriptor(TableTypeConstants.TYPE_PAGE_TWO);
/* 1165 */       if (page2 != null) {
/* 1166 */         attrs = page2.getAttributes();
/* 1167 */         for (int i = 0; i < attrs.length; i++) {
/* 1168 */           IAttribute attr = attrs[i];
/* 1169 */           if (isVisible(attr)) {
/* 1170 */             result.put(
/* 1171 */                 Integer.valueOf(Integer.parseInt(attr.getId().toString())), attr);
/*      */           }
/*      */         } 
/*      */       } 
/*      */ 
/*      */ 
/*      */       
/* 1178 */       ITableDesc page3 = cls.getTableDescriptor(TableTypeConstants.TYPE_PAGE_THREE);
/* 1179 */       if (page3 != null) {
/* 1180 */         attrs = page3.getAttributes();
/* 1181 */         for (int i = 0; i < attrs.length; i++) {
/* 1182 */           IAttribute attr = attrs[i];
/* 1183 */           if (isVisible(attr)) {
/* 1184 */             result.put(
/* 1185 */                 Integer.valueOf(Integer.parseInt(attr.getId().toString())), attr);
/*      */           }
/*      */         } 
/*      */       } 
/*      */     } 
/*      */ 
/*      */     
/* 1192 */     return result;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   public static Map<Integer, IAttribute> getRequiredAttributes(IAgileSession session, String itemType) throws APIException {
/* 1205 */     Map<Integer, IAttribute> result = new HashMap<>();
/* 1206 */     Map<Integer, IAttribute> temp = getVisibleAttributes(session, itemType);
/* 1207 */     for (Map.Entry<Integer, IAttribute> entry : temp.entrySet()) {
/* 1208 */       Integer id = entry.getKey();
/* 1209 */       IAttribute attr = entry.getValue();
/* 1210 */       if (isRequired(attr)) {
/* 1211 */         result.put(id, attr);
/*      */       }
/*      */     } 
/* 1214 */     return result;
/*      */   }
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */ 
/*      */   
/*      */   private static List<String> getObsoleteListValues(IAgileSession session, IAttribute attr) throws APIException {
/* 1225 */     List<String> result = new ArrayList<>();
/*      */     
/* 1227 */     IProperty propList = attr.getProperty(PropertyConstants.PROP_LIST);
/*      */ 
/*      */     
/* 1230 */     if (propList != null) {
/* 1231 */       String listName = propList.getValue().toString();
/* 1232 */       if (listName != null && !listName.equals("")) {
/*      */         
/* 1234 */         IAdminList adminList = session.getAdminInstance().getListLibrary().getAdminList(listName);
/* 1235 */         if (adminList != null) {
/* 1236 */           IAgileList list = adminList.getValues();
/* 1237 */           List<String> listValue = new ArrayList<>();
/*      */           
/* 1239 */           IAgileList[] lists = (IAgileList[])list.getChildren();
/* 1240 */           if (lists != null) {
/* 1241 */             for (IAgileList tempList : lists) {
/* 1242 */               if (tempList.getValue() != null) {
/* 1243 */                 listValue.add(tempList.getValue().toString());
/*      */               }
/*      */             } 
/*      */           }
/*      */         } 
/*      */       } 
/*      */     } 
/*      */ 
/*      */     
/* 1252 */     return result;
/*      */   }
/*      */ }


/* Location:              {{SOURCE_PATH}}\com\jiexin\pl\\util\AgileUtil.class
 * Java compiler version: 8 (52.0)
 * JD-Core Version:       1.1.3
 */