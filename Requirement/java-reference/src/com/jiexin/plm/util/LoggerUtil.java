/*    */ package com.jiexin.plm.util;
/*    */ 
/*    */ import com.agile.api.APIException;
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ public class LoggerUtil
/*    */ {
/*    */   public static String getLoggerUtil() throws APIException {
/* 15 */     String logXML = "";
/*    */     try {
/* 17 */       String env = ConfigUtil.getPropertyValue("env");
/* 18 */       if (env.equals("dev")) {
/* 19 */         logXML = "log4j2.xml";
/* 20 */       } else if (env.equals("test")) {
/* 21 */         logXML = "E:/jiexinFile/log4j2.xml";
/* 22 */       } else if (env.equals("pro")) {
/* 23 */         logXML = "D:/plm/log4j2.xml";
/*    */       } 
/* 25 */       System.out.println("获取日志地址 : " + logXML);
/* 26 */     } catch (Exception e) {
/* 27 */       e.printStackTrace();
/* 28 */       throw new RuntimeException("获取日志地址异常", e);
/*    */     } 
/* 30 */     if (logXML.equals("")) {
/* 31 */       throw new RuntimeException("获取日志地址失效！");
/*    */     }
/* 33 */     return logXML;
/*    */   }
/*    */ 
/*    */   
/*    */   public static void main(String[] args) throws APIException {
/* 38 */     String loggerUtil = getLoggerUtil();
/*    */   }
/*    */ }


/* Location:              {{SOURCE_PATH}}\com\jiexin\pl\\util\LoggerUtil.class
 * Java compiler version: 8 (52.0)
 * JD-Core Version:       1.1.3
 */