/*    */ package com.jiexin.plm.util;
/*    */ 
/*    */ import java.io.IOException;
/*    */ import java.io.InputStream;
/*    */ import org.apache.ibatis.io.Resources;
/*    */ import org.apache.ibatis.session.SqlSession;
/*    */ import org.apache.ibatis.session.SqlSessionFactory;
/*    */ import org.apache.ibatis.session.SqlSessionFactoryBuilder;
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ public class MybatisUtil
/*    */ {
/*    */   public static SqlSession sqlSessionFactory() throws IOException {
/* 21 */     InputStream inputStream = Resources.getResourceAsStream("mybatis-config.xml");
/*    */     
/* 23 */     SqlSessionFactory factory = (new SqlSessionFactoryBuilder()).build(inputStream);
/*    */     
/* 25 */     return factory.openSession();
/*    */   }
/*    */ }


/* Location:              {{SOURCE_PATH}}\com\jiexin\pl\\util\MybatisUtil.class
 * Java compiler version: 8 (52.0)
 * JD-Core Version:       1.1.3
 */