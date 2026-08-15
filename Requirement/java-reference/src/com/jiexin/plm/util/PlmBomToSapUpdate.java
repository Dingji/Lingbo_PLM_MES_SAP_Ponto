/*     */ package com.jiexin.plm.util;
/*     */ import com.sap.conn.jco.JCoDestination;
/*     */ import com.sap.conn.jco.JCoDestinationManager;
/*     */ import com.sap.conn.jco.JCoException;
/*     */ import com.sap.conn.jco.JCoFunction;
/*     */ import com.sap.conn.jco.JCoParameterList;
/*     */ import com.sap.conn.jco.JCoTable;
/*     */ import java.io.File;
/*     */ import java.io.FileOutputStream;
/*     */ import java.util.ArrayList;
/*     */ import java.util.HashMap;
/*     */ import java.util.List;
/*     */ import java.util.Map;
/*     */ import java.util.Properties;
/*     */ import org.apache.logging.log4j.core.Logger;
/*     */ import org.apache.logging.log4j.core.LoggerContext;
/*     */ import org.apache.logging.log4j.core.config.Configurator;
/*     */ 
/*     */ public class PlmBomToSapUpdate {
/*     */   private static final String client = "SAP.CLIENT";
/*     */   private static final String user = "SAP.USER";
/*     */   private static final String passwd = "SAP.PASSWORD";
/*     */   private static final String lang = "SAP.LANG";
/*  24 */   private static JCoDestination destination = null; private static final String ashost = "SAP.ASHOST"; private static final String sysnr = "SAP.SYSNR"; private static final String ABAP_AS_POOLED = "ABAP_AS_WITH_POOL"; public static final String SAP_BOM = "SAP.BOM.UPDATE"; public static final String SAP_BOM_TABLE = "SAP.BOM.REQ";
/*  25 */   private static Logger logger = null;
/*     */ 
/*     */   
/*     */   static {
/*  29 */     Properties connectProperties = new Properties();
/*  30 */     connectProperties.setProperty("jco.client.sysnr", PropertiesUtil.getPropertyValue("SAP.SYSNR"));
/*  31 */     connectProperties.setProperty("jco.client.client", PropertiesUtil.getPropertyValue("SAP.CLIENT"));
/*  32 */     connectProperties.setProperty("jco.client.user", PropertiesUtil.getPropertyValue("SAP.USER"));
/*  33 */     connectProperties.setProperty("jco.client.passwd", PropertiesUtil.getPropertyValue("SAP.PASSWORD"));
/*  34 */     connectProperties.setProperty("jco.client.lang", PropertiesUtil.getPropertyValue("SAP.LANG"));
/*  35 */     connectProperties.setProperty("jco.destination.pool_capacity", "5");
/*  36 */     connectProperties.setProperty("jco.destination.peak_limit", "10");
/*     */     
/*  38 */     String env = ConfigUtil.getPropertyValue("env");
/*  39 */     if (env.equals("pro")) {
/*  40 */       connectProperties.setProperty("jco.client.msserv", "3600");
/*  41 */       connectProperties.setProperty("jco.client.mshost", "10.149.10.134");
/*  42 */       connectProperties.setProperty("jco.client.r3name", "SFP");
/*  43 */       connectProperties.setProperty("jco.client.group", "SAPERP_SDFICO");
/*     */     } else {
/*  45 */       connectProperties.setProperty("jco.client.ashost", PropertiesUtil.getPropertyValue("SAP.ASHOST"));
/*  46 */       System.out.println("连接SAP测试环境无需负载均衡!!!");
/*     */     } 
/*  48 */     createDataFile("ABAP_AS_WITH_POOL", "jcoDestination", connectProperties);
/*     */     
/*     */     try {
/*  51 */       String logXML = PropertiesUtil.getPropertyValue("LOG.URL");
/*  52 */       LoggerContext ctx = Configurator.initialize(null, (new File(logXML)).toURI().toString());
/*  53 */       logger = ctx.getLogger(PlmBomToSapUpdate.class.getName());
/*  54 */     } catch (Exception ex) {
/*  55 */       ex.printStackTrace();
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
/*  67 */     File cfg = new File(name + "." + suffix);
/*  68 */     if (cfg.exists()) {
/*  69 */       cfg.deleteOnExit();
/*     */     }
/*     */     try {
/*  72 */       FileOutputStream fos = new FileOutputStream(cfg, false);
/*  73 */       properties.store(fos, "for tests only !");
/*  74 */       fos.close();
/*  75 */     } catch (Exception e) {
/*  76 */       throw new RuntimeException("Unable to create the destination file " + cfg.getName(), e);
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
/*  87 */       if (null == destination)
/*  88 */         destination = JCoDestinationManager.getDestination("ABAP_AS_WITH_POOL"); 
/*  89 */       destination.ping();
/*  90 */     } catch (JCoException e) {
/*  91 */       e.printStackTrace();
/*     */     } 
/*  93 */     return destination;
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
/*     */   public static Map<String, List<String>> plmToSapBom(List<Map<String, String>> mapList) throws Exception {
/* 105 */     logger.info("传输修改BOM到SAP");
/* 106 */     Map<String, List<String>> returnmap = new HashMap<>();
/*     */     
/* 108 */     JCoDestination connect = connect();
/* 109 */     JCoFunction bapi = connect.getRepository().getFunction(PropertiesUtil.getPropertyValue("SAP.BOM.UPDATE"));
/* 110 */     JCoParameterList inputlist = bapi.getTableParameterList();
/*     */     
/* 112 */     JCoTable structure = inputlist.getTable(PropertiesUtil.getPropertyValue("SAP.BOM.REQ"));
/* 113 */     logger.info("mapList   [{}]", mapList);
/*     */     
/* 115 */     for (Map<String, String> map : mapList) {
/* 116 */       structure.appendRow();
/* 117 */       for (Map.Entry<String, String> entry : map.entrySet()) {
/* 118 */         String name = entry.getKey();
/* 119 */         String id = entry.getValue();
/* 120 */         structure.setValue(name, id);
/*     */       } 
/*     */     } 
/*     */     
/* 124 */     bapi.execute(connect);
/*     */     
/* 126 */     JCoParameterList exportParameter = bapi.getTableParameterList();
/* 127 */     JCoTable resultTable = exportParameter.getTable("ET_ERROR");
/* 128 */     List<String> success = new ArrayList<>();
/* 129 */     List<String> error = new ArrayList<>();
/* 130 */     for (int i = 0; i < resultTable.getNumRows(); i++) {
/* 131 */       resultTable.setRow(i);
/* 132 */       String itemNumber = resultTable.getString("SUBNUM");
/* 133 */       String partNumber = resultTable.getString("PARENTNUM");
/* 134 */       String status = resultTable.getString("Z_EXEFLAG");
/* 135 */       String message = resultTable.getString("Z_EXEMSG");
/* 136 */       if (status.equals("S")) {
/* 137 */         logger.info("父物料:" + partNumber + "修改BOM物料" + itemNumber + "：" + message);
/* 138 */         success.add("父物料:" + partNumber + "修改BOM物料" + itemNumber + "：" + message);
/*     */       } else {
/* 140 */         logger.info("父物料:" + partNumber + "修改BOM ERROR： 物料" + itemNumber + "：" + message);
/* 141 */         error.add("父物料:" + partNumber + "修改BOM ERROR：物料" + itemNumber + "：" + message);
/*     */       } 
/*     */     } 
/* 144 */     returnmap.put("S", success);
/* 145 */     returnmap.put("E", error);
/*     */     
/* 147 */     bapi.clone();
/* 148 */     return returnmap;
/*     */   }
/*     */ }


/* Location:              {{SOURCE_PATH}}\com\jiexin\pl\\util\PlmBomToSapUpdate.class
 * Java compiler version: 8 (52.0)
 * JD-Core Version:       1.1.3
 */