/*    */ package com.jiexin.plm.util;
/*    */ 
/*    */ import java.io.File;
/*    */ import java.io.IOException;
/*    */ import java.nio.charset.StandardCharsets;
/*    */ import org.apache.http.HttpEntity;
/*    */ import org.apache.http.client.methods.HttpPost;
/*    */ import org.apache.http.entity.StringEntity;
/*    */ import org.apache.http.impl.client.CloseableHttpClient;
/*    */ import org.apache.http.impl.client.HttpClients;
/*    */ import org.apache.logging.log4j.Logger;
/*    */ import org.apache.logging.log4j.core.LoggerContext;
/*    */ import org.apache.logging.log4j.core.config.Configurator;
/*    */ 
/*    */ 
/*    */ 
/*    */ public class MdmUtil
/*    */ {
/* 19 */   public static Logger logger = null;
/*    */ 
/*    */   
/* 22 */   private static String MDM_URL = PropertiesUtil.getPropertyValue("MDM.URL");
/* 23 */   private static String ACCEPT = "application/json";
/*    */ 
/*    */   
/* 26 */   private static String X_MDM_CLIENT_CODE = "LBPLM_MAT_MDM";
/*    */   
/* 28 */   private static String X_MDM_CLIENT_SECRET_TEST = "{{MDM_CLIENT_SECRET_TEST}}";
/*    */ 
/*    */ 
/*    */   
/* 32 */   private static String X_MDM_CLIENT_SECRET_PRO = "{{MDM_CLIENT_SECRET_PROD}}";
/*    */   static {
/*    */     try {
/* 35 */       String logXML = PropertiesUtil.getPropertyValue("LOG.URL");
/* 36 */       LoggerContext loggerContext = Configurator.initialize(null, (new File(logXML)).toURI().toString());
/* 37 */       logger = (Logger)loggerContext.getLogger(MdmUtil.class.getName());
/* 38 */     } catch (Exception ex) {
/* 39 */       ex.printStackTrace();
/*    */     } 
/*    */     
/* 42 */     if (MDM_URL == null || MDM_URL.trim().isEmpty())
/* 43 */       throw new IllegalStateException("配置项 'mdm.url' 未设置或为空。"); 
/*    */   }
/*    */   
/*    */   public static String sendPost(String requestBody) throws IOException {
/* 47 */     logger.info("MDM Url is :" + MDM_URL);
/*    */     
/* 49 */     try (CloseableHttpClient httpClient = HttpClients.createDefault()) {
/*    */       
/* 51 */       HttpPost httpPost = new HttpPost(MDM_URL);
/*    */ 
/*    */       
/* 54 */       httpPost.setHeader("Accept", ACCEPT);
/* 55 */       httpPost.setHeader("x-mdm-client-code", X_MDM_CLIENT_CODE);
/* 56 */       String env = ConfigUtil.getPropertyValue("env");
/* 57 */       if (env.equals("pro")) {
/* 58 */         httpPost.setHeader("x-mdm-client-secret", X_MDM_CLIENT_SECRET_PRO);
/* 59 */         System.out.println("正在向MDM正式环境发送集成请求");
/*    */       } else {
/* 61 */         httpPost.setHeader("x-mdm-client-secret", X_MDM_CLIENT_SECRET_TEST);
/*    */         
/* 63 */         System.out.println("正在向MDM测试环境发送集成请求");
/*    */       } 
/*    */ 
/*    */       
/* 67 */       httpPost.setHeader("Content-Type", "application/json;charset=UTF-8");
/*    */ 
/*    */       
/* 70 */       StringEntity requestEntity = new StringEntity(requestBody, StandardCharsets.UTF_8);
/* 71 */       httpPost.setEntity((HttpEntity)requestEntity);
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */     
/*    */     }
/* 93 */     catch (IOException e) {
/* 94 */       e.printStackTrace();
/*    */       
/* 96 */       throw new RuntimeException("Failed to send POST request to MDM.", e);
/*    */     } 
/*    */   }
/*    */ }


/* Location:              {{SOURCE_PATH}}\com\jiexin\pl\\util\MdmUtil.class
 * Java compiler version: 8 (52.0)
 * JD-Core Version:       1.1.3
 */