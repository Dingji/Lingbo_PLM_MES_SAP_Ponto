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
/*     */ 
/*     */ public class PlmBomToSap {
/*     */   private static final String client = "SAP.CLIENT";
/*     */   private static final String user = "SAP.USER";
/*     */   private static final String passwd = "SAP.PASSWORD";
/*     */   private static final String lang = "SAP.LANG";
/*  23 */   private static JCoDestination destination = null; private static final String ashost = "SAP.ASHOST"; private static final String sysnr = "SAP.SYSNR"; private static final String ABAP_AS_POOLED = "ABAP_AS_WITH_POOL"; public static final String SAP_BOM = "SAP.BOM.CREATE"; public static final String SAP_BOM_TABLE = "SAP.BOM.CREATE-REQ";
/*  24 */   private static Logger logger = null;
/*     */   
/*     */   static {
/*  27 */     Properties connectProperties = new Properties();
/*  28 */     connectProperties.setProperty("jco.client.sysnr", PropertiesUtil.getPropertyValue("SAP.SYSNR"));
/*  29 */     connectProperties.setProperty("jco.client.client", PropertiesUtil.getPropertyValue("SAP.CLIENT"));
/*  30 */     connectProperties.setProperty("jco.client.user", PropertiesUtil.getPropertyValue("SAP.USER"));
/*  31 */     connectProperties.setProperty("jco.client.passwd", PropertiesUtil.getPropertyValue("SAP.PASSWORD"));
/*  32 */     connectProperties.setProperty("jco.client.lang", PropertiesUtil.getPropertyValue("SAP.LANG"));
/*  33 */     connectProperties.setProperty("jco.destination.pool_capacity", "5");
/*  34 */     connectProperties.setProperty("jco.destination.peak_limit", "10");
/*     */     
/*  36 */     String env = ConfigUtil.getPropertyValue("env");
/*  37 */     if (env.equals("pro")) {
/*  38 */       connectProperties.setProperty("jco.client.msserv", "3600");
/*  39 */       connectProperties.setProperty("jco.client.mshost", "10.149.10.134");
/*  40 */       connectProperties.setProperty("jco.client.r3name", "SFP");
/*  41 */       connectProperties.setProperty("jco.client.group", "SAPERP_SDFICO");
/*     */     } else {
/*  43 */       connectProperties.setProperty("jco.client.ashost", PropertiesUtil.getPropertyValue("SAP.ASHOST"));
/*  44 */       System.out.println("连接SAP测试环境无需负载均衡!!!");
/*     */     } 
/*  46 */     createDataFile("ABAP_AS_WITH_POOL", "jcoDestination", connectProperties);
/*     */ 
/*     */     
/*     */     try {
/*  50 */       String logXML = PropertiesUtil.getPropertyValue("LOG.URL");
/*  51 */       LoggerContext ctx = Configurator.initialize(null, (new File(logXML)).toURI().toString());
/*  52 */       logger = ctx.getLogger(PlmBomToSap.class.getName());
/*  53 */     } catch (Exception ex) {
/*  54 */       ex.printStackTrace();
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
/*  66 */     File cfg = new File(name + "." + suffix);
/*  67 */     if (cfg.exists()) {
/*  68 */       cfg.deleteOnExit();
/*     */     }
/*     */     try {
/*  71 */       FileOutputStream fos = new FileOutputStream(cfg, false);
/*  72 */       properties.store(fos, "for tests only !");
/*  73 */       fos.close();
/*  74 */     } catch (Exception e) {
/*  75 */       throw new RuntimeException("Unable to create the destination file " + cfg.getName(), e);
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
/*  86 */       if (null == destination)
/*  87 */         destination = JCoDestinationManager.getDestination("ABAP_AS_WITH_POOL"); 
/*  88 */       destination.ping();
/*  89 */     } catch (JCoException e) {
/*  90 */       e.printStackTrace();
/*     */     } 
/*  92 */     return destination;
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
/* 104 */     System.out.println(" =========== 开始向SAP抛送数据====================== ");
/* 105 */     Map<String, List<String>> returnmap = new HashMap<>();
/*     */     
/* 107 */     JCoDestination connect = connect();
/* 108 */     JCoFunction bapi = connect.getRepository().getFunction(PropertiesUtil.getPropertyValue("SAP.BOM.CREATE"));
/* 109 */     JCoParameterList inputlist = bapi.getTableParameterList();
/*     */     
/* 111 */     JCoTable structure = inputlist.getTable(PropertiesUtil.getPropertyValue("SAP.BOM.CREATE-REQ"));
/*     */ 
/*     */     
/* 114 */     for (Map<String, String> map : mapList) {
/* 115 */       structure.appendRow();
/* 116 */       for (Map.Entry<String, String> entry : map.entrySet()) {
/* 117 */         String name = entry.getKey();
/* 118 */         String id = entry.getValue();
/* 119 */         structure.setValue(name, id);
/*     */       } 
/*     */     } 
/*     */     
/* 123 */     bapi.execute(connect);
/*     */     
/* 125 */     JCoParameterList exportParameter = bapi.getTableParameterList();
/* 126 */     JCoTable resultTable = exportParameter.getTable("ET_ERROR");
/* 127 */     List<String> success = new ArrayList<>();
/* 128 */     List<String> error = new ArrayList<>();
/* 129 */     for (int i = 0; i < resultTable.getNumRows(); i++) {
/* 130 */       resultTable.setRow(i);
/* 131 */       String itemNumber = resultTable.getString("SUBNUM");
/* 132 */       String partNumber = resultTable.getString("PARENTNUM");
/* 133 */       String status = resultTable.getString("Z_EXEFLAG");
/* 134 */       String message = resultTable.getString("Z_EXEMSG");
/* 135 */       if (status.equals("S")) {
/* 136 */         logger.info("父物料:" + partNumber + "新建BOM物料" + itemNumber + "：" + message);
/* 137 */         success.add("父物料:" + partNumber + "新建BOM物料" + itemNumber + "：" + message);
/*     */       } else {
/* 139 */         logger.error("父物料:" + partNumber + "新建BOM ERROR： 物料" + itemNumber + "：" + message);
/* 140 */         error.add("父物料:" + partNumber + "新建BOM ERROR：物料" + itemNumber + "：" + message);
/*     */       } 
/*     */     } 
/* 143 */     returnmap.put("S", success);
/* 144 */     returnmap.put("E", error);
/*     */     
/* 146 */     bapi.clone();
/*     */     
/* 148 */     return returnmap;
/*     */   }
/*     */ }


/* Location:              C:\Users\dingj\Desktop\in\123\!\com\jiexin\pl\\util\PlmBomToSap.class
 * Java compiler version: 8 (52.0)
 * JD-Core Version:       1.1.3
 */