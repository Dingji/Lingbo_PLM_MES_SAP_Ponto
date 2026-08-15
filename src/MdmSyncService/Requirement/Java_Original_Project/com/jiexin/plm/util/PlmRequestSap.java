/*     */ package com.jiexin.plm.util;
/*     */ import com.sap.conn.jco.JCoDestination;
/*     */ import com.sap.conn.jco.JCoFunction;
/*     */ import com.sap.conn.jco.JCoParameterList;
/*     */ import com.sap.conn.jco.JCoTable;
/*     */ import java.io.File;
/*     */ import java.io.FileOutputStream;
/*     */ import java.util.ArrayList;
/*     */ import java.util.List;
/*     */ import java.util.Map;
/*     */ import java.util.Properties;
/*     */ 
/*     */ public class PlmRequestSap {
/*  14 */   private static Logger logger = null;
/*     */   
/*     */   private static final String client = "SAP.CLIENT";
/*     */   private static final String user = "SAP.USER";
/*     */   private static final String passwd = "SAP.PASSWORD";
/*     */   private static final String lang = "SAP.LANG";
/*     */   private static final String ashost = "SAP.ASHOST";
/*     */   private static final String sysnr = "SAP.SYSNR";
/*     */   private static final String ABAP_AS_POOLED = "ABAP_AS_WITH_POOL";
/*     */   public static final String SAP_ITEM = "SAP.ITEM";
/*     */   public static final String SAP_ITEM_TABLE = "SAP.REQ";
/*  25 */   private static JCoDestination destination = null;
/*     */   
/*     */   static {
/*  28 */     Properties connectProperties = new Properties();
/*  29 */     connectProperties.setProperty("jco.client.sysnr", PropertiesUtil.getPropertyValue("SAP.SYSNR"));
/*  30 */     connectProperties.setProperty("jco.client.client", PropertiesUtil.getPropertyValue("SAP.CLIENT"));
/*  31 */     connectProperties.setProperty("jco.client.user", PropertiesUtil.getPropertyValue("SAP.USER"));
/*  32 */     connectProperties.setProperty("jco.client.passwd", PropertiesUtil.getPropertyValue("SAP.PASSWORD"));
/*  33 */     connectProperties.setProperty("jco.client.lang", PropertiesUtil.getPropertyValue("SAP.LANG"));
/*  34 */     connectProperties.setProperty("jco.destination.pool_capacity", "5");
/*  35 */     connectProperties.setProperty("jco.destination.peak_limit", "10");
/*     */     
/*  37 */     String env = ConfigUtil.getPropertyValue("env");
/*  38 */     if (env.equals("pro")) {
/*  39 */       connectProperties.setProperty("jco.client.msserv", "3600");
/*  40 */       connectProperties.setProperty("jco.client.mshost", "10.149.10.134");
/*  41 */       connectProperties.setProperty("jco.client.r3name", "SFP");
/*  42 */       connectProperties.setProperty("jco.client.group", "SAPERP_SDFICO");
/*     */     } else {
/*  44 */       connectProperties.setProperty("jco.client.ashost", PropertiesUtil.getPropertyValue("SAP.ASHOST"));
/*  45 */       System.out.println("连接SAP测试环境无需负载均衡!!!");
/*     */     } 
/*     */     
/*  48 */     createDataFile("ABAP_AS_WITH_POOL", "jcoDestination", connectProperties);
/*     */ 
/*     */     
/*     */     try {
/*  52 */       String logXML = PropertiesUtil.getPropertyValue("LOG.URL");
/*  53 */       LoggerContext loggerContext = Configurator.initialize(null, (new File(logXML)).toURI().toString());
/*  54 */       logger = (Logger)loggerContext.getLogger(PlmRequestSap.class.getName());
/*  55 */     } catch (Exception ex) {
/*  56 */       ex.printStackTrace();
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
/*     */   private static void createDataFile(String name, String suffix, Properties properties) {
/*  68 */     File cfg = new File(name + "." + suffix);
/*  69 */     if (cfg.exists()) {
/*  70 */       cfg.deleteOnExit();
/*     */     }
/*     */     try {
/*  73 */       FileOutputStream fos = new FileOutputStream(cfg, false);
/*  74 */       properties.store(fos, "for tests only !");
/*  75 */       fos.close();
/*  76 */     } catch (Exception e) {
/*  77 */       throw new RuntimeException("Unable to create the destination file " + cfg.getName(), e);
/*     */     } 
/*     */   }
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */   
/*     */   public static JCoDestination connect() {
/*     */     try {
/*  88 */       if (null == destination)
/*  89 */         destination = JCoDestinationManager.getDestination("ABAP_AS_WITH_POOL"); 
/*  90 */       destination.ping();
/*  91 */     } catch (JCoException e) {
/*  92 */       e.printStackTrace();
/*     */     } 
/*  94 */     return destination;
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
/*     */   public static Map<String, List<String>> plmToSapItem(List<Map<String, Object>> mapList) throws Exception {
/* 106 */     Map<String, List<String>> returnmap = new HashMap<>();
/*     */     
/* 108 */     JCoDestination connect = connect();
/* 109 */     JCoFunction bapi = connect.getRepository().getFunction(PropertiesUtil.getPropertyValue("SAP.ITEM"));
/* 110 */     JCoParameterList inputlist = bapi.getTableParameterList();
/*     */     
/* 112 */     JCoTable structure = inputlist.getTable(PropertiesUtil.getPropertyValue("SAP.REQ"));
/*     */ 
/*     */ 
/*     */     
/* 116 */     JCoTable structureList = inputlist.getTable("IT_LIST");
/*     */     
/* 118 */     for (Map<String, Object> map : mapList) {
/* 119 */       structure.appendRow();
/*     */ 
/*     */ 
/*     */       
/* 123 */       for (Map.Entry<String, Object> entry : map.entrySet()) {
/* 124 */         String name = entry.getKey();
/* 125 */         structure.setValue(name, entry.getValue());
/*     */       } 
/*     */       
/* 128 */       structureList.appendRow();
/* 129 */       structureList.setValue("BISMT", map.get("BISMT"));
/* 130 */       structureList.setValue("ZCWB", map.get("ZCWB"));
/* 131 */       structureList.setValue("SPRAS", map.get("SPRAS"));
/* 132 */       structureList.setValue("SPRAS", "ZH");
/*     */     } 
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
/* 145 */     bapi.execute(connect);
/*     */     
/* 147 */     JCoParameterList exportParameter = bapi.getTableParameterList();
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */     
/* 155 */     JCoTable resultTable = exportParameter.getTable("ET_DATA");
/*     */     
/* 157 */     List<String> success = new ArrayList<>();
/* 158 */     List<String> error = new ArrayList<>();
/* 159 */     for (int i = 0; i < resultTable.getNumRows(); i++) {
/* 160 */       resultTable.setRow(i);
/* 161 */       String itemNumber = resultTable.getString("MATNR");
/* 162 */       String status = resultTable.getString("E_STATUS");
/* 163 */       String message = resultTable.getString("E_MSGTXT");
/* 164 */       if (status.equals("S")) {
/* 165 */         logger.info("物料" + itemNumber + "：" + message);
/* 166 */         success.add(itemNumber);
/*     */       } else {
/* 168 */         logger.info("物料" + itemNumber + "：" + message);
/* 169 */         error.add(message);
/*     */       } 
/*     */     } 
/* 172 */     returnmap.put("S", success);
/* 173 */     returnmap.put("E", error);
/*     */     
/* 175 */     bapi.clone();
/* 176 */     return returnmap;
/*     */   }
/*     */ }


/* Location:              C:\Users\dingj\Desktop\in\123\!\com\jiexin\pl\\util\PlmRequestSap.class
 * Java compiler version: 8 (52.0)
 * JD-Core Version:       1.1.3
 */