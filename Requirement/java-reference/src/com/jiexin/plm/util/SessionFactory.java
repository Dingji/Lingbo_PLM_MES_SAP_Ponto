/*     */ package com.jiexin.plm.util;
/*     */ 
/*     */ import com.agile.api.APIException;
/*     */ import com.agile.api.AgileSessionFactory;
/*     */ import com.agile.api.IAgileSession;
/*     */ import java.util.Calendar;
/*     */ import java.util.HashMap;
/*     */ import java.util.Map;
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ public class SessionFactory
/*     */ {
/*  15 */   private static AgileSessionFactory sessionFactory = null;
/*     */ 
/*     */ 
/*     */   
/*     */   private static final String AGILEURL = "AGILE.URL";
/*     */ 
/*     */ 
/*     */   
/*     */   private static final String AGILEUSER = "AGILE.USER";
/*     */ 
/*     */ 
/*     */   
/*     */   private static final String AGILEPASSWORD = "AGILE.PASSWORD";
/*     */ 
/*     */ 
/*     */   
/*  31 */   private static Calendar iniDate = null;
/*     */ 
/*     */ 
/*     */   
/*  35 */   private static IAgileSession session = null;
/*     */   
/*     */   static {
/*     */     try {
/*  39 */       InitialSessionFactory();
/*  40 */     } catch (APIException e) {
/*  41 */       e.printStackTrace();
/*     */     } 
/*     */   }
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */   
/*     */   private static void InitialSessionFactory() throws APIException {
/*  50 */     if (sessionFactory == null) {
/*  51 */       String agileUrl = PropertiesUtil.getPropertyValue("AGILE.URL");
/*     */       
/*  53 */       sessionFactory = AgileSessionFactory.refreshInstance(agileUrl);
/*     */     } 
/*     */   }
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */   
/*     */   public static IAgileSession getSession() throws APIException {
/*  63 */     if (session == null || !session.isOpen() || iniDate == null) {
/*  64 */       iniSession();
/*     */ 
/*     */     
/*     */     }
/*     */     else {
/*     */ 
/*     */ 
/*     */       
/*  72 */       session = null;
/*  73 */       iniSession();
/*     */     } 
/*     */ 
/*     */     
/*  77 */     return session;
/*     */   }
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */   
/*     */   public static IAgileSession getSpecifySession(String userName, String password) throws APIException {
/*  86 */     if (sessionFactory == null)
/*     */     {
/*  88 */       throw APIException.createException(new Throwable("SessionFactory is null."));
/*     */     }
/*  90 */     Map<Integer, String> params = new HashMap<>();
/*  91 */     params.put(AgileSessionFactory.USERNAME, userName);
/*  92 */     params.put(AgileSessionFactory.PASSWORD, password);
/*     */     
/*  94 */     IAgileSession tempSession = sessionFactory.createSession(params);
/*  95 */     return tempSession;
/*     */   }
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */   
/*     */   private static IAgileSession iniSession() throws APIException {
/* 103 */     if (sessionFactory == null) {
/* 104 */       throw APIException.createException(new Throwable("SessionFactory is null."));
/*     */     }
/* 106 */     String agileUser = PropertiesUtil.getPropertyValue("AGILE.USER");
/* 107 */     String agilePassword = PropertiesUtil.getPropertyValue("AGILE.PASSWORD");
/* 108 */     Map<Integer, String> params = new HashMap<>();
/* 109 */     params.put(AgileSessionFactory.USERNAME, agileUser);
/* 110 */     params.put(AgileSessionFactory.PASSWORD, agilePassword);
/* 111 */     session = sessionFactory.createSession(params);
/* 112 */     iniDate = Calendar.getInstance();
/*     */     
/* 114 */     return session;
/*     */   }
/*     */   
/*     */   public static void main(String[] args) {
/*     */     try {
/* 119 */       IAgileSession iAgileSession = getSession();
/* 120 */     } catch (APIException e) {
/*     */       
/* 122 */       e.printStackTrace();
/*     */     } 
/*     */   }
/*     */ }


/* Location:              {{SOURCE_PATH}}\com\jiexin\pl\\util\SessionFactory.class
 * Java compiler version: 8 (52.0)
 * JD-Core Version:       1.1.3
 */