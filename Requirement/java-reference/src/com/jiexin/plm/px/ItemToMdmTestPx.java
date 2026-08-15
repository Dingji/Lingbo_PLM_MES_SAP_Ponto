/*     */ package com.jiexin.plm.px;
/*     */ import com.agile.api.APIException;
/*     */ import com.agile.api.ChangeConstants;
/*     */ import com.agile.api.IAgileSession;
/*     */ import com.agile.api.IChange;
/*     */ import com.agile.api.IDataObject;
/*     */ import com.agile.api.IItem;
/*     */ import com.agile.api.INode;
/*     */ import com.agile.api.IRow;
/*     */ import com.agile.api.ITable;
/*     */ import com.agile.api.ItemConstants;
/*     */ import com.agile.px.ActionResult;
/*     */ import com.agile.px.ICustomAction;
/*     */ import com.alibaba.fastjson.JSON;
/*     */ import com.alibaba.fastjson.JSONObject;
/*     */ import com.jiexin.plm.entity.MdmItem;
/*     */ import com.jiexin.plm.util.AgileUtil;
/*     */ import com.jiexin.plm.util.LoggerUtil;
/*     */ import java.io.IOException;
/*     */ import java.util.ArrayList;
/*     */ import java.util.List;
/*     */ import org.apache.logging.log4j.core.Logger;
/*     */ import org.apache.logging.log4j.core.LoggerContext;
/*     */ 
/*     */ public class ItemToMdmTestPx implements ICustomAction {
/*  26 */   private static Logger logger = null;
/*     */ 
/*     */   
/*     */   static {
/*     */     try {
/*  31 */       String logXML = LoggerUtil.getLoggerUtil();
/*     */       
/*  33 */       LoggerContext ctx = Configurator.initialize(null, (new File(logXML)).toURI().toString());
/*  34 */       logger = ctx.getLogger(ItemToMdmTestPx.class.getName());
/*  35 */     } catch (Exception ex) {
/*  36 */       ex.printStackTrace();
/*     */     } 
/*     */   }
/*     */ 
/*     */   
/*     */   public ActionResult doAction(IAgileSession iAgileSession, INode iNode, IDataObject iDataObject) {
/*  42 */     IChange change = (IChange)iDataObject;
/*     */     try {
/*  44 */       String s = addItem(change);
/*  45 */       return new ActionResult(0, s);
/*     */     }
/*  47 */     catch (Exception e) {
/*  48 */       e.printStackTrace();
/*  49 */       return new ActionResult(-1, e);
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
/*     */   public static String subFirstN(String str, int n) {
/*  62 */     if (str == null) {
/*  63 */       return "";
/*     */     }
/*     */     
/*  66 */     int len = Math.min(str.length(), n);
/*  67 */     return str.substring(0, len);
/*     */   }
/*     */   
/*     */   private static String addItem(IChange change) throws RuntimeException, APIException {
/*  71 */     ITable table = change.getTable(ChangeConstants.TABLE_AFFECTEDITEMS);
/*  72 */     int size = table.size();
/*  73 */     if (size <= 0) {
/*  74 */       logger.error("当前变更单没有受影响物件");
/*     */     }
/*  76 */     List<MdmItem> mdmItemList = new ArrayList<>();
/*     */     
/*  78 */     for (Object o : table) {
/*  79 */       IRow row = (IRow)o;
/*  80 */       IItem item = (IItem)row.getReferent();
/*  81 */       String apiName = item.getAgileClass().getSuperClass().getAPIName();
/*  82 */       if (apiName.equals("PartsClass")) {
/*  83 */         addSapItem(item, row, mdmItemList);
/*     */       }
/*     */     } 
/*     */ 
/*     */     
/*  88 */     if (mdmItemList.size() > 0) {
/*  89 */       logger.info("向MDM 新增物料：" + mdmItemList);
/*     */       try {
/*  91 */         String result = MdmUtil.sendPost(JSON.toJSONString(mdmItemList));
/*  92 */         JSONObject jsonObject = JSON.parseObject(result);
/*     */ 
/*     */         
/*  95 */         Integer code = jsonObject.getInteger("code");
/*  96 */         if (code == null) {
/*  97 */           throw new RuntimeException("未获取到请求结果状态！！！");
/*     */         }
/*  99 */         String message = jsonObject.getString("message");
/* 100 */         if (code.intValue() != 200) {
/* 101 */           throw new RuntimeException("MDM 集成失败！！！ 原因：" + message);
/*     */         }
/* 103 */         logger.info("返回的message：" + message);
/* 104 */         return result;
/* 105 */       } catch (IOException e) {
/* 106 */         e.printStackTrace();
/* 107 */         throw new RuntimeException("物料抛送MDM异常！！！", e);
/*     */       } 
/*     */     } 
/* 110 */     return "当前变更单暂时没有对应数据！！！";
/*     */   }
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */   
/*     */   private static void addSapItem(IItem item, IRow row, List<MdmItem> mdmItemList) {
/* 121 */     MdmItem mdmItem = new MdmItem();
/*     */     
/*     */     try {
/* 124 */       String desc = AgileUtil.getValue((IDataObject)item, Integer.valueOf(1002));
/* 125 */       mdmItem.setName(desc);
/*     */ 
/*     */       
/* 128 */       String itemOldNumber = AgileUtil.getValue((IDataObject)item, Integer.valueOf(2011));
/*     */       
/* 130 */       mdmItem.setNumber(itemOldNumber);
/*     */ 
/*     */ 
/*     */       
/* 134 */       String oneSelectApi = AgileUtil.getRedlineListApi(item, Integer.valueOf(1274));
/* 135 */       mdmItem.setMaterial_type(oneSelectApi);
/*     */ 
/*     */       
/* 138 */       String itemNumber = item.getName();
/*     */       
/* 140 */       mdmItem.setNew_sap_number(itemNumber);
/*     */ 
/*     */ 
/*     */       
/* 144 */       String lev = AgileUtil.getRedlineListApi(item, Integer.valueOf(1275));
/*     */       
/* 146 */       mdmItem.setNew_product_code2(lev);
/*     */ 
/*     */       
/* 149 */       String name = item.getAgileClass().getName();
/* 150 */       String matkl = subFirstN(name, 5);
/*     */       
/* 152 */       mdmItem.setNew_material_group(matkl);
/*     */       
/* 154 */       String mein = AgileUtil.getValue((IDataObject)item, Integer.valueOf(1271));
/*     */       
/* 156 */       mdmItem.setBasic_units(mein);
/*     */ 
/*     */       
/* 159 */       String lifeType = row.getCell(Integer.valueOf(1057)).getValue().toString();
/* 160 */       if (lifeType.equals("停用") || lifeType.equals("EOL")) {
/*     */ 
/*     */         
/* 163 */         mdmItem.setState("淘汰");
/*     */       
/*     */       }
/*     */       else {
/*     */         
/* 168 */         mdmItem.setState("在产");
/*     */       } 
/*     */ 
/*     */       
/* 172 */       String werks = AgileUtil.getRedlineValueByItem(item, Integer.valueOf(2090));
/* 173 */       mdmItem.setFactory(werks);
/*     */       
/* 175 */       mdmItem.setIndustry_sector("M");
/* 176 */       mdmItem.setProduct_description2("配件");
/* 177 */       String isMdm = item.getCell(ItemConstants.ATT_PAGE_TWO_LIST01).getValue().toString();
/* 178 */       if (isMdm.equals("Y")) {
/* 179 */         mdmItem.setParts_mat_type("雅迪零部件");
/*     */       } else {
/* 181 */         mdmItem.setParts_mat_type("凌博零部件");
/*     */       } 
/* 183 */       mdmItemList.add(mdmItem);
/* 184 */     } catch (APIException e) {
/* 185 */       e.printStackTrace();
/* 186 */       throw new RuntimeException("获取属性异常", e);
/*     */     } 
/*     */   }
/*     */ 
/*     */   
/*     */   public static void main(String[] args) throws APIException {
/* 192 */     IAgileSession session = SessionFactory.getSession();
/* 193 */     IItem object = (IItem)session.getObject(2, "X9841-0000426");
/* 194 */     Object value = object.getCell(ItemConstants.ATT_PAGE_TWO_MULTILIST01).getValue();
/* 195 */     object.setValue(ItemConstants.ATT_PAGE_TWO_MULTILIST02, value);
/*     */   }
/*     */ }


/* Location:              {{SOURCE_PATH}}\com\jiexin\plm\px\ItemToMdmTestPx.class
 * Java compiler version: 8 (52.0)
 * JD-Core Version:       1.1.3
 */