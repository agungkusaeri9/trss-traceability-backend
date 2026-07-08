-- MySQL dump 10.13  Distrib 8.0.30, for Win64 (x86_64)
--
-- Host: localhost    Database: trss_traceability_system
-- ------------------------------------------------------
-- Server version	8.0.30

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `__efmigrationshistory`
--

DROP TABLE IF EXISTS `__efmigrationshistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `__efmigrationshistory` (
  `MigrationId` varchar(150) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ProductVersion` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `__efmigrationshistory`
--

LOCK TABLES `__efmigrationshistory` WRITE;
/*!40000 ALTER TABLE `__efmigrationshistory` DISABLE KEYS */;
INSERT INTO `__efmigrationshistory` VALUES ('20260626024914_InitialCreate','9.0.0'),('20260630074454_AddOrderToProcessAndParameter','9.0.0'),('20260630083022_AddStockInReworkTable','9.0.0'),('20260630085934_AddStatusToProcessLogDetail','9.0.0'),('20260703032633_AddIsFinishedToProcessLog','9.0.0');
/*!40000 ALTER TABLE `__efmigrationshistory` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `app_configs`
--

DROP TABLE IF EXISTS `app_configs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `app_configs` (
  `id` int NOT NULL AUTO_INCREMENT,
  `key` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `value` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `description` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `created_at` datetime(6) NOT NULL,
  `updated_at` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `app_configs`
--

LOCK TABLES `app_configs` WRITE;
/*!40000 ALTER TABLE `app_configs` DISABLE KEYS */;
INSERT INTO `app_configs` VALUES (1,'PRINTER_NAME_STOCK_IN','Canon E470 series','Printer name for Stock In process.','2026-06-26 02:51:16.326448',NULL),(2,'PRINTER_NAME_CLINCHING','ZDesigner ZT231-203dpi ZPL','Printer Name for Clinching Process','2026-06-26 02:51:16.326448',NULL);
/*!40000 ALTER TABLE `app_configs` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `issue_transactions`
--

DROP TABLE IF EXISTS `issue_transactions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `issue_transactions` (
  `id` int NOT NULL AUTO_INCREMENT,
  `issue_id` int NOT NULL,
  `qty_before` decimal(18,2) NOT NULL,
  `qty_change` decimal(18,2) NOT NULL,
  `qty_after` decimal(18,2) NOT NULL,
  `type` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `remark` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `created_at` datetime(6) NOT NULL,
  PRIMARY KEY (`id`),
  KEY `ix_issue_transactions_issue_id` (`issue_id`),
  CONSTRAINT `fk_issue_transactions_issues_issue_id` FOREIGN KEY (`issue_id`) REFERENCES `issues` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=501 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `issue_transactions`
--

LOCK TABLES `issue_transactions` WRITE;
/*!40000 ALTER TABLE `issue_transactions` DISABLE KEYS */;
INSERT INTO `issue_transactions` VALUES (329,32,100.00,-1.00,99.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-02 03:06:22.993803'),(330,33,50.00,-1.00,49.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-02 03:06:23.112723'),(331,34,70.00,-1.00,69.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-02 03:06:23.122170'),(332,32,99.00,-1.00,98.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-02 03:53:50.883889'),(333,33,49.00,-1.00,48.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-02 03:53:51.086273'),(334,34,69.00,-1.00,68.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-02 03:53:51.110209'),(335,32,98.00,-1.00,97.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-02 03:57:38.367375'),(336,33,48.00,-1.00,47.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-02 03:57:38.566371'),(337,34,68.00,-1.00,67.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-02 03:57:38.586967'),(338,32,97.00,-1.00,96.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-02 03:58:44.145085'),(339,33,47.00,-1.00,46.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-02 03:58:44.312598'),(340,34,67.00,-1.00,66.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-02 03:58:44.323857'),(341,32,96.00,-1.00,95.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-02 04:02:59.346994'),(342,33,46.00,-1.00,45.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-02 04:02:59.658113'),(343,34,66.00,-1.00,65.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-02 04:02:59.676662'),(344,32,95.00,-1.00,94.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-02 04:03:16.244879'),(345,33,45.00,-1.00,44.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-02 04:03:16.254943'),(346,34,65.00,-1.00,64.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-02 04:03:16.266419'),(347,32,94.00,-1.00,93.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-02 04:04:54.781374'),(348,33,44.00,-1.00,43.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-02 04:04:54.788446'),(349,34,64.00,-1.00,63.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-02 04:04:54.796101'),(350,32,93.00,-1.00,92.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-02 04:29:13.615695'),(351,33,43.00,-1.00,42.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-02 04:29:13.768651'),(352,34,63.00,-1.00,62.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-02 04:29:13.778284'),(353,35,40.00,-1.00,39.00,'ISSUE','Serial number batch create (MFan, 1 pcs)','2026-07-02 04:29:50.712093'),(354,36,220.00,-1.00,219.00,'ISSUE','Serial number batch create (MFan, 1 pcs)','2026-07-02 04:29:50.720785'),(355,39,220.00,-1.00,219.00,'ISSUE','Serial number batch create (MFan, 1 pcs)','2026-07-02 04:29:50.727525'),(356,32,92.00,-1.00,91.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-02 07:31:16.719887'),(357,33,42.00,-1.00,41.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-02 07:31:16.976480'),(358,34,62.00,-1.00,61.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-02 07:31:16.990499'),(359,35,39.00,-1.00,38.00,'ISSUE','Serial number batch create (MFan, 1 pcs)','2026-07-02 07:31:29.490778'),(360,36,219.00,-1.00,218.00,'ISSUE','Serial number batch create (MFan, 1 pcs)','2026-07-02 07:31:29.501336'),(361,39,219.00,-1.00,218.00,'ISSUE','Serial number batch create (MFan, 1 pcs)','2026-07-02 07:31:29.509989'),(362,32,91.00,-1.00,90.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-02 07:32:20.137243'),(363,33,41.00,-1.00,40.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-02 07:32:20.148119'),(364,34,61.00,-1.00,60.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-02 07:32:20.155388'),(365,32,90.00,-1.00,89.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-03 03:37:31.852215'),(366,33,40.00,-1.00,39.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-03 03:37:31.974814'),(367,34,60.00,-1.00,59.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-03 03:37:31.989050'),(368,35,38.00,-1.00,37.00,'ISSUE','Serial number batch create (MFan, 1 pcs)','2026-07-03 03:39:19.404841'),(369,36,218.00,-1.00,217.00,'ISSUE','Serial number batch create (MFan, 1 pcs)','2026-07-03 03:39:19.410576'),(370,39,218.00,-1.00,217.00,'ISSUE','Serial number batch create (MFan, 1 pcs)','2026-07-03 03:39:19.419082'),(371,32,89.00,-1.00,88.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-03 03:39:49.516994'),(372,33,39.00,-1.00,38.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-03 03:39:49.525200'),(373,34,59.00,-1.00,58.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-03 03:39:49.532014'),(374,35,37.00,-1.00,36.00,'ISSUE','Serial number batch create (MFan, 1 pcs)','2026-07-03 03:44:58.382522'),(375,36,217.00,-1.00,216.00,'ISSUE','Serial number batch create (MFan, 1 pcs)','2026-07-03 03:44:58.392428'),(376,39,217.00,-1.00,216.00,'ISSUE','Serial number batch create (MFan, 1 pcs)','2026-07-03 03:44:58.397502'),(377,32,88.00,-1.00,87.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-03 03:50:36.882238'),(378,33,38.00,-1.00,37.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-03 03:50:37.113871'),(379,34,58.00,-1.00,57.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-03 03:50:37.133830'),(380,35,36.00,-1.00,35.00,'ISSUE','Serial number batch create (MFan, 1 pcs)','2026-07-03 03:50:55.411269'),(381,36,216.00,-1.00,215.00,'ISSUE','Serial number batch create (MFan, 1 pcs)','2026-07-03 03:50:55.417361'),(382,39,216.00,-1.00,215.00,'ISSUE','Serial number batch create (MFan, 1 pcs)','2026-07-03 03:50:55.423286'),(383,32,87.00,-1.00,86.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-03 03:57:51.922333'),(384,33,37.00,-1.00,36.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-03 03:57:52.044173'),(385,34,57.00,-1.00,56.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-03 03:57:52.054107'),(386,32,86.00,-1.00,85.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 01:36:48.413508'),(387,33,36.00,-1.00,35.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 01:36:48.755055'),(388,32,85.00,-1.00,84.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 01:37:19.447687'),(389,33,35.00,-1.00,34.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 01:37:19.457353'),(390,34,56.00,-1.00,55.00,'ISSUE','Serial number batch create (MFan, 1 pcs)','2026-07-07 01:37:58.440121'),(391,35,35.00,-1.00,34.00,'ISSUE','Serial number batch create (MFan, 1 pcs)','2026-07-07 01:37:58.445157'),(392,36,215.00,-1.00,214.00,'ISSUE','Serial number batch create (MFan, 1 pcs)','2026-07-07 01:37:58.450337'),(393,32,84.00,-1.00,83.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 01:51:26.753630'),(394,33,34.00,-1.00,33.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 01:51:26.869437'),(395,34,55.00,-1.00,54.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 01:51:26.876308'),(396,35,34.00,-1.00,33.00,'ISSUE','Serial number batch create (MFan, 1 pcs)','2026-07-07 01:51:52.722443'),(397,36,214.00,-1.00,213.00,'ISSUE','Serial number batch create (MFan, 1 pcs)','2026-07-07 01:51:52.729976'),(398,39,215.00,-1.00,214.00,'ISSUE','Serial number batch create (MFan, 1 pcs)','2026-07-07 01:51:52.737330'),(399,32,83.00,-1.00,82.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 01:53:54.324542'),(400,33,33.00,-1.00,32.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 01:53:54.333907'),(401,34,54.00,-1.00,53.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 01:53:54.341974'),(402,35,33.00,-1.00,32.00,'ISSUE','Serial number batch create (MFan, 1 pcs)','2026-07-07 01:54:05.682851'),(403,36,213.00,-1.00,212.00,'ISSUE','Serial number batch create (MFan, 1 pcs)','2026-07-07 01:54:05.693163'),(404,39,214.00,-1.00,213.00,'ISSUE','Serial number batch create (MFan, 1 pcs)','2026-07-07 01:54:05.699643'),(405,32,82.00,-1.00,81.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 02:02:25.943005'),(406,33,32.00,-1.00,31.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 02:02:26.316496'),(407,34,53.00,-1.00,52.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 02:02:26.332240'),(408,35,32.00,-1.00,31.00,'ISSUE','Serial number batch create (MFan, 1 pcs)','2026-07-07 02:02:43.661542'),(409,36,212.00,-1.00,211.00,'ISSUE','Serial number batch create (MFan, 1 pcs)','2026-07-07 02:02:43.668358'),(410,39,213.00,-1.00,212.00,'ISSUE','Serial number batch create (MFan, 1 pcs)','2026-07-07 02:02:43.674418'),(411,32,81.00,-1.00,80.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 02:05:42.386934'),(412,33,31.00,-1.00,30.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 02:05:42.395495'),(413,34,52.00,-1.00,51.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 02:05:42.400763'),(414,32,80.00,-1.00,79.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 02:06:15.614594'),(415,33,30.00,-1.00,29.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 02:06:15.620986'),(416,34,51.00,-1.00,50.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 02:06:15.627484'),(417,32,79.00,-1.00,78.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 02:07:53.199702'),(418,33,29.00,-1.00,28.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 02:07:53.205810'),(419,34,50.00,-1.00,49.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 02:07:53.213642'),(420,32,78.00,-1.00,77.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 02:09:10.764466'),(421,33,28.00,-1.00,27.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 02:09:10.774325'),(422,34,49.00,-1.00,48.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 02:09:10.782161'),(423,32,77.00,-1.00,76.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 02:13:27.787821'),(424,33,27.00,-1.00,26.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 02:13:27.796072'),(425,34,48.00,-1.00,47.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 02:13:27.803296'),(426,32,76.00,-1.00,75.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 02:17:10.539171'),(427,33,26.00,-1.00,25.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 02:17:10.546666'),(428,34,47.00,-1.00,46.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 02:17:10.556051'),(429,35,31.00,-1.00,30.00,'ISSUE','Serial number batch create (MFan, 1 pcs)','2026-07-07 02:17:26.454178'),(430,36,211.00,-1.00,210.00,'ISSUE','Serial number batch create (MFan, 1 pcs)','2026-07-07 02:17:26.460097'),(431,39,212.00,-1.00,211.00,'ISSUE','Serial number batch create (MFan, 1 pcs)','2026-07-07 02:17:26.465041'),(432,32,75.00,-1.00,74.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:20:10.967580'),(433,33,25.00,-1.00,24.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:20:11.299220'),(434,34,46.00,-1.00,45.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:20:11.320465'),(435,32,74.00,-1.00,73.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:23:05.998993'),(436,33,24.00,-1.00,23.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:23:06.147127'),(437,34,45.00,-1.00,44.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:23:06.159842'),(438,32,73.00,-1.00,72.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:25:18.741195'),(439,33,23.00,-1.00,22.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:25:18.914830'),(440,34,44.00,-1.00,43.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:25:18.925671'),(441,32,72.00,-1.00,71.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:28:13.030909'),(442,33,22.00,-1.00,21.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:28:13.153215'),(443,34,43.00,-1.00,42.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:28:13.161862'),(444,32,71.00,-1.00,70.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:33:11.187766'),(445,33,21.00,-1.00,20.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:33:11.272755'),(446,34,42.00,-1.00,41.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:33:11.286494'),(447,32,70.00,-1.00,69.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:33:34.860954'),(448,33,20.00,-1.00,19.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:33:34.872282'),(449,34,41.00,-1.00,40.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:33:34.886860'),(450,32,69.00,-1.00,68.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:35:40.608738'),(451,33,19.00,-1.00,18.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:35:40.614520'),(452,34,40.00,-1.00,39.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:35:40.621713'),(453,32,68.00,-1.00,67.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:36:34.838794'),(454,33,18.00,-1.00,17.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:36:34.850884'),(455,34,39.00,-1.00,38.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:36:34.862244'),(456,32,67.00,-1.00,66.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:49:28.357041'),(457,33,17.00,-1.00,16.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:49:28.563600'),(458,34,38.00,-1.00,37.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:49:28.576181'),(459,32,66.00,-1.00,65.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:49:53.165185'),(460,33,16.00,-1.00,15.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:49:53.177760'),(461,34,37.00,-1.00,36.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:49:53.202407'),(462,32,65.00,-1.00,64.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:52:41.194012'),(463,33,15.00,-1.00,14.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:52:41.380422'),(464,34,36.00,-1.00,35.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:52:41.397586'),(465,32,64.00,-1.00,63.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:55:32.631378'),(466,33,14.00,-1.00,13.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:55:32.870998'),(467,34,35.00,-1.00,34.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:55:32.887775'),(468,32,63.00,-1.00,62.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:59:06.147642'),(469,33,13.00,-1.00,12.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:59:06.348170'),(470,34,34.00,-1.00,33.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 08:59:06.357997'),(471,32,62.00,-1.00,61.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 09:03:42.352280'),(472,33,12.00,-1.00,11.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 09:03:42.744925'),(473,34,33.00,-1.00,32.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 09:03:42.767441'),(474,32,61.00,-1.00,60.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 09:05:25.880174'),(475,33,11.00,-1.00,10.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 09:05:26.009108'),(476,34,32.00,-1.00,31.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 09:05:26.019453'),(477,32,60.00,-1.00,59.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 09:07:19.330296'),(478,33,10.00,-1.00,9.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 09:07:19.728806'),(479,34,31.00,-1.00,30.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 09:07:19.759552'),(480,32,59.00,-1.00,58.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 09:10:40.711586'),(481,33,9.00,-1.00,8.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 09:10:40.842812'),(482,34,30.00,-1.00,29.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 09:10:40.853302'),(483,32,58.00,-1.00,57.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 09:12:56.921582'),(484,33,8.00,-1.00,7.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 09:12:57.314636'),(485,34,29.00,-1.00,28.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 09:12:57.340858'),(486,32,57.00,-1.00,56.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 09:25:45.816230'),(487,33,7.00,-1.00,6.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 09:25:45.934345'),(488,34,28.00,-1.00,27.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 09:25:45.947422'),(489,32,56.00,-1.00,55.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 09:27:44.030477'),(490,33,6.00,-1.00,5.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 09:27:44.553588'),(491,34,27.00,-1.00,26.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 09:27:44.598144'),(492,32,55.00,-1.00,54.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 09:29:32.049568'),(493,33,5.00,-1.00,4.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 09:29:32.583919'),(494,34,26.00,-1.00,25.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 09:29:32.617106'),(495,32,54.00,-1.00,53.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 09:36:17.057272'),(496,33,4.00,-1.00,3.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 09:36:17.612128'),(497,34,25.00,-1.00,24.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 09:36:17.647422'),(498,32,53.00,-1.00,52.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 09:36:40.915476'),(499,33,3.00,-1.00,2.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 09:36:40.947011'),(500,34,24.00,-1.00,23.00,'ISSUE','Serial number batch create (Clinching, 1 pcs)','2026-07-07 09:36:41.015492');
/*!40000 ALTER TABLE `issue_transactions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `issues`
--

DROP TABLE IF EXISTS `issues`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `issues` (
  `id` int NOT NULL AUTO_INCREMENT,
  `number` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `stock_in_id` int NOT NULL,
  `created_at` datetime(6) NOT NULL,
  `updated_at` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ix_issues_number` (`number`),
  KEY `ix_issues_stock_in_id` (`stock_in_id`),
  CONSTRAINT `fk_issues_stock_ins_stock_in_id` FOREIGN KEY (`stock_in_id`) REFERENCES `stock_ins` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=40 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `issues`
--

LOCK TABLES `issues` WRITE;
/*!40000 ALTER TABLE `issues` DISABLE KEYS */;
INSERT INTO `issues` VALUES (32,'20260702001',32,'2026-07-02 01:39:15.623289',NULL),(33,'20260702002',33,'2026-07-02 01:39:36.796022',NULL),(34,'20260702003',34,'2026-07-02 01:39:49.026789',NULL),(35,'20260702004',35,'2026-07-02 01:40:01.265893',NULL),(36,'20260702005',36,'2026-07-02 01:40:14.235755',NULL),(39,'20260702006',39,'2026-07-02 01:44:02.587217',NULL);
/*!40000 ALTER TABLE `issues` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `mqtt_print_requests`
--

DROP TABLE IF EXISTS `mqtt_print_requests`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `mqtt_print_requests` (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `process_code` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `issue_number` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `raw_payload` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `status` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `error_message` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `created_at` datetime(6) NOT NULL,
  `processed_at` datetime(6) DEFAULT NULL,
  `updated_at` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `mqtt_print_requests`
--

LOCK TABLES `mqtt_print_requests` WRITE;
/*!40000 ALTER TABLE `mqtt_print_requests` DISABLE KEYS */;
INSERT INTO `mqtt_print_requests` VALUES (1,'CLINCHING_SHORT_SIDE','','{\n    \"issue_number\": \"\"\n}','Processed',NULL,'2026-06-26 03:14:41.503027','2026-06-26 03:14:41.550750','2026-06-26 03:14:41.550834'),(2,'CLINCHING_SHORT_SIDE','','{\n    \"issue_number\": \"\"\n}','Processed',NULL,'2026-06-26 03:14:42.800326','2026-06-26 03:14:42.823141','2026-06-26 03:14:42.823227');
/*!40000 ALTER TABLE `mqtt_print_requests` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `parameters`
--

DROP TABLE IF EXISTS `parameters`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `parameters` (
  `id` int NOT NULL AUTO_INCREMENT,
  `code` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `description` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `data_type` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `is_active` tinyint(1) NOT NULL DEFAULT '1',
  `created_at` datetime(6) NOT NULL,
  `updated_at` datetime(6) DEFAULT NULL,
  `order` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ix_parameters_code` (`code`)
) ENGINE=InnoDB AUTO_INCREMENT=136 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `parameters`
--

LOCK TABLES `parameters` WRITE;
/*!40000 ALTER TABLE `parameters` DISABLE KEYS */;
INSERT INTO `parameters` VALUES (106,'CORE_ASM_RESULT','Core Asm',NULL,'boolean',1,'2026-06-08 09:50:46.372805',NULL,1),(107,'UPPER_TANK_ASM_RESULT','Upper Tank Asm Result',NULL,'boolean',1,'2026-06-08 09:50:46.372851',NULL,2),(108,'LOWER_TANK_ASM_RESULT','Lower Tank Asm Result',NULL,'boolean',1,'2026-06-08 09:50:46.372861',NULL,3),(109,'CLINCHING_HEIGHT_RESULT','Clinching Height Result',NULL,'boolean',1,'2026-06-08 09:50:46.372870',NULL,1),(110,'CLINCHING_HEIGHT_VALUE','Clinching Height Value',NULL,'number',1,'2026-06-08 09:50:46.372872',NULL,2),(111,'END_PLATE_WIDTH_VALUE','End Plate Width Value',NULL,'number',1,'2026-06-08 09:50:46.372874',NULL,3),(112,'CAP_TYPE_POSITION_RESULT','Cap Type & Position',NULL,'boolean',1,'2026-06-08 09:50:46.372878',NULL,1),(113,'LEAK_TEST_RESULT','Leak Test Result',NULL,'boolean',1,'2026-06-08 09:50:46.372880',NULL,2),(114,'LEAK_VALUE','Leak Value',NULL,'number',1,'2026-06-08 09:50:46.372882',NULL,3),(115,'FAN_ASM_RESULT','Fan Asm Result',NULL,'boolean',1,'2026-06-08 09:50:46.372885',NULL,1),(116,'MOTOR_ASM_RESULT','Motor Asm Result',NULL,'boolean',1,'2026-06-08 09:50:46.372888',NULL,2),(117,'FUN_GUIDE_ASM_RESULT','Fun Guide Asm Result',NULL,'boolean',1,'2026-06-08 09:50:46.372889',NULL,3),(118,'BOLT_TIGHTEN_RESULT','Bolt Tighten Result',NULL,'boolean',1,'2026-06-08 09:50:46.372890',NULL,4),(119,'BOLT_TIGHTEN_VALUE','Bolt Tighten Value',NULL,'number',1,'2026-06-08 09:50:46.372892',NULL,5),(120,'NUT_TIGHTEN_RESULT','Nut Tighten Result',NULL,'boolean',1,'2026-06-08 09:50:46.372894',NULL,6),(121,'M_FAN_TEST_RESULT','M Fan Test Result',NULL,'boolean',1,'2026-06-08 09:50:46.372897',NULL,1),(122,'M_FAN_INSPECTION_ROTATION_SPEED_VALUE','M Fan Inspection Rotation Speed Value',NULL,'number',1,'2026-06-08 09:50:46.372899',NULL,2),(123,'M_FAN_INSPECTION_AMPERE_VALUE','M Fan Inspection Amperage Value',NULL,'number',1,'2026-06-08 09:50:46.372900',NULL,5),(124,'M_FAN_INSPECTION_WIND_DIRECTION_VALUE','M Fan Inspection Wind Direction Value',NULL,'number',1,'2026-06-08 09:50:46.372903',NULL,8),(125,'RAD_CORE_ASM_NAME_LABEL_RESULT','Rad Core Asm Name Label Result',NULL,'boolean',1,'2026-06-08 09:50:46.372916',NULL,1),(126,'MOTOR_FAN_ASSY_LABEL_RESULT','Motor Fan Assy Label Result',NULL,'boolean',1,'2026-06-08 09:50:46.372917',NULL,2),(127,'ECM_ASSY_BOLT_TIGHTEN_VALUE','ECM Assy Bolt Tighten Value',NULL,'number',1,'2026-06-08 09:50:46.372919',NULL,3),(128,'ECM_ASSY_BOLT_TIGHTEN_RESULT','ECM Assy Bolt Tighten Result',NULL,'boolean',1,'2026-06-08 09:50:46.372920',NULL,4),(129,'FINAL_INSPECTION_RAD_CORE_ASM_NAME_LABEL_RESULT','Final Inspection Rad Core Asm Name Label Result',NULL,'boolean',1,'2026-06-08 09:50:46.372925',NULL,1),(130,'ALL_CHECK_POINT_RESULT','All Check Point Result',NULL,'boolean',1,'2026-06-08 09:50:46.372926',NULL,2),(131,'M_FAN_INSPECTION_ROTATION_SPEED_MAX_VALUE','Rotation Speed Max','Max value for Rotation Speed','number',1,'2026-06-30 07:32:45.000000',NULL,3),(132,'M_FAN_INSPECTION_ROTATION_SPEED_MIN_VALUE','Rotation Speed Min','Min value for Rotation Speed','number',1,'2026-06-30 07:32:45.000000',NULL,4),(133,'M_FAN_INSPECTION_AMPERE_MAX_VALUE','Ampere Max','Max value for Ampere','number',1,'2026-06-30 07:32:45.000000',NULL,6),(134,'M_FAN_INSPECTION_AMPERE_MIN_VALUE','Ampere Min','Min value for Ampere','number',1,'2026-06-30 07:32:45.000000',NULL,7);
/*!40000 ALTER TABLE `parameters` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `parts`
--

DROP TABLE IF EXISTS `parts`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `parts` (
  `id` int NOT NULL AUTO_INCREMENT,
  `number` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `name` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `description` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL DEFAULT '1',
  `created_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=102 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `parts`
--

LOCK TABLES `parts` WRITE;
/*!40000 ALTER TABLE `parts` DISABLE KEYS */;
INSERT INTO `parts` VALUES (1,'P001','UPPER TANK','Upper Tank',1,'2026-06-26 02:51:16',NULL),(2,'P002','LOWER TANK','Lower Tank',1,'2026-06-26 02:51:16',NULL),(3,'P003','CORE ASM','Core Asm',1,'2026-06-26 02:51:16',NULL),(8,'P004','FAN ASM','Fan Asm',1,'2026-06-26 02:51:16',NULL),(9,'P005','MOTOR ASM','Motor Asm',1,'2026-06-26 02:51:16',NULL),(10,'P006','FUN GUIDE ASM','Fun Guide Asm',1,'2026-06-26 02:51:16',NULL);
/*!40000 ALTER TABLE `parts` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `printers`
--

DROP TABLE IF EXISTS `printers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `printers` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ip_address` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `port` int NOT NULL,
  `description` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL,
  `created_at` datetime(6) NOT NULL,
  `updated_at` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `printers`
--

LOCK TABLES `printers` WRITE;
/*!40000 ALTER TABLE `printers` DISABLE KEYS */;
INSERT INTO `printers` VALUES (1,'Printer-Line-01','192.168.1.101',9100,'Label printer for Stamping Press line 1.',0,'2026-06-26 02:51:16.287126','2026-07-08 08:00:59.796578'),(2,'Printer-Line-02','192.168.1.102',9100,'Label printer for Fin Mill line 2.',0,'2026-06-26 02:51:16.287127','2026-07-08 08:00:59.796578'),(3,'Printer-Line-03','192.168.1.103',9100,'Label printer for Tube Mill line 3.',0,'2026-06-26 02:51:16.287127','2026-07-08 08:00:59.796578'),(4,'Printer-Line-04','192.168.1.104',9100,'Label printer for Core Assembly station.',0,'2026-06-26 02:51:16.287127','2026-07-08 08:00:59.796579'),(5,'Printer-Line-05','192.168.1.105',9100,'Label printer for Brazing Furnace exit.',0,'2026-06-26 02:51:16.287127','2026-07-08 08:00:59.796579');
/*!40000 ALTER TABLE `printers` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `process_log_details`
--

DROP TABLE IF EXISTS `process_log_details`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `process_log_details` (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `process_log_id` bigint NOT NULL,
  `process_id` int NOT NULL,
  `parameter_id` int NOT NULL,
  `value_number` decimal(65,30) DEFAULT NULL,
  `value_text` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `value_boolean` tinyint(1) DEFAULT NULL,
  `created_at` datetime(6) NOT NULL,
  `updated_at` datetime(6) DEFAULT NULL,
  `status` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`id`),
  KEY `ix_process_log_details_parameter_id` (`parameter_id`),
  KEY `ix_process_log_details_process_id` (`process_id`),
  KEY `ix_process_log_details_process_log_id` (`process_log_id`),
  CONSTRAINT `fk_process_log_details_parameters_parameter_id` FOREIGN KEY (`parameter_id`) REFERENCES `parameters` (`id`) ON DELETE RESTRICT,
  CONSTRAINT `fk_process_log_details_process_logs_process_log_id` FOREIGN KEY (`process_log_id`) REFERENCES `process_logs` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_process_log_details_processes_process_id` FOREIGN KEY (`process_id`) REFERENCES `processes` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=193 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `process_log_details`
--

LOCK TABLES `process_log_details` WRITE;
/*!40000 ALTER TABLE `process_log_details` DISABLE KEYS */;
INSERT INTO `process_log_details` VALUES (1,1,30,106,NULL,NULL,1,'2026-07-07 01:53:54.412981',NULL,1),(2,1,30,107,NULL,NULL,1,'2026-07-07 01:53:54.412982',NULL,1),(3,1,30,108,NULL,NULL,1,'2026-07-07 01:53:54.412982',NULL,1),(4,1,31,109,NULL,NULL,1,'2026-07-07 01:53:59.686797',NULL,1),(5,1,31,110,1.230000000000000000000000000000,NULL,NULL,'2026-07-07 01:53:59.686797',NULL,1),(6,1,31,111,45.670000000000000000000000000000,NULL,NULL,'2026-07-07 01:53:59.686798',NULL,1),(7,1,32,112,NULL,NULL,1,'2026-07-07 01:54:02.585455',NULL,1),(8,1,32,113,NULL,NULL,1,'2026-07-07 01:54:02.585456',NULL,1),(9,1,32,114,45.670000000000000000000000000000,NULL,NULL,'2026-07-07 01:54:02.585456',NULL,1),(10,2,33,106,NULL,NULL,1,'2026-07-07 01:54:05.760571',NULL,1),(11,2,33,107,NULL,NULL,1,'2026-07-07 01:54:05.760570',NULL,1),(12,2,33,108,NULL,NULL,1,'2026-07-07 01:54:05.760569',NULL,1),(13,2,33,118,NULL,NULL,1,'2026-07-07 01:54:10.368373',NULL,1),(14,2,33,119,45.670000000000000000000000000000,NULL,NULL,'2026-07-07 01:54:10.368374',NULL,1),(15,2,33,120,NULL,NULL,1,'2026-07-07 01:54:10.368374',NULL,1),(16,2,34,121,NULL,NULL,1,'2026-07-07 01:54:13.225344',NULL,1),(17,2,34,122,60.000000000000000000000000000000,NULL,NULL,'2026-07-07 01:54:13.225345',NULL,1),(18,2,34,131,100.000000000000000000000000000000,NULL,NULL,'2026-07-07 01:54:13.225345',NULL,1),(19,2,34,132,40.000000000000000000000000000000,NULL,NULL,'2026-07-07 01:54:13.225345',NULL,1),(20,2,34,123,89.000000000000000000000000000000,NULL,NULL,'2026-07-07 01:54:13.225345',NULL,1),(21,2,34,133,120.000000000000000000000000000000,NULL,NULL,'2026-07-07 01:54:13.225346',NULL,1),(22,2,34,134,60.000000000000000000000000000000,NULL,NULL,'2026-07-07 01:54:13.225346',NULL,1),(23,2,34,124,45.000000000000000000000000000000,NULL,NULL,'2026-07-07 01:54:13.225346',NULL,1),(24,1,35,125,NULL,NULL,1,'2026-07-07 01:54:20.374433',NULL,1),(25,1,35,126,NULL,NULL,1,'2026-07-07 01:54:20.374434',NULL,1),(26,1,35,127,45.670000000000000000000000000000,NULL,NULL,'2026-07-07 01:54:20.374435',NULL,1),(27,1,35,128,NULL,NULL,1,'2026-07-07 01:54:20.374435',NULL,1),(28,1,36,129,NULL,NULL,1,'2026-07-07 01:54:23.336061',NULL,1),(29,1,36,130,NULL,NULL,1,'2026-07-07 01:54:23.336062',NULL,1),(30,3,30,106,NULL,NULL,1,'2026-07-07 02:02:26.842941',NULL,1),(31,3,30,107,NULL,NULL,1,'2026-07-07 02:02:26.842943',NULL,1),(32,3,30,108,NULL,NULL,1,'2026-07-07 02:02:26.842943',NULL,1),(33,3,31,109,NULL,NULL,1,'2026-07-07 02:02:38.484468',NULL,1),(34,3,31,110,1.230000000000000000000000000000,NULL,NULL,'2026-07-07 02:02:38.484469',NULL,1),(35,3,31,111,45.670000000000000000000000000000,NULL,NULL,'2026-07-07 02:02:38.484469',NULL,1),(36,3,32,112,NULL,NULL,1,'2026-07-07 02:02:40.893714',NULL,1),(37,3,32,113,NULL,NULL,1,'2026-07-07 02:02:40.893715',NULL,1),(38,3,32,114,45.670000000000000000000000000000,NULL,NULL,'2026-07-07 02:02:40.893716',NULL,1),(39,4,33,106,NULL,NULL,1,'2026-07-07 02:02:43.782541',NULL,1),(40,4,33,107,NULL,NULL,1,'2026-07-07 02:02:43.782541',NULL,1),(41,4,33,108,NULL,NULL,1,'2026-07-07 02:02:43.782540',NULL,1),(42,4,33,118,NULL,NULL,1,'2026-07-07 02:02:48.641116',NULL,1),(43,4,33,119,45.670000000000000000000000000000,NULL,NULL,'2026-07-07 02:02:48.641117',NULL,1),(44,4,33,120,NULL,NULL,1,'2026-07-07 02:02:48.641117',NULL,1),(45,4,34,121,NULL,NULL,1,'2026-07-07 02:02:54.530943',NULL,1),(46,4,34,122,60.000000000000000000000000000000,NULL,NULL,'2026-07-07 02:02:54.530943',NULL,1),(47,4,34,131,100.000000000000000000000000000000,NULL,NULL,'2026-07-07 02:02:54.530943',NULL,1),(48,4,34,132,40.000000000000000000000000000000,NULL,NULL,'2026-07-07 02:02:54.530943',NULL,1),(49,4,34,123,89.000000000000000000000000000000,NULL,NULL,'2026-07-07 02:02:54.530943',NULL,1),(50,4,34,133,120.000000000000000000000000000000,NULL,NULL,'2026-07-07 02:02:54.530943',NULL,1),(51,4,34,134,60.000000000000000000000000000000,NULL,NULL,'2026-07-07 02:02:54.530944',NULL,1),(52,4,34,124,45.000000000000000000000000000000,NULL,NULL,'2026-07-07 02:02:54.530944',NULL,1),(53,3,35,125,NULL,NULL,1,'2026-07-07 02:03:00.933750',NULL,1),(54,3,35,126,NULL,NULL,1,'2026-07-07 02:03:00.933750',NULL,1),(55,3,35,127,45.670000000000000000000000000000,NULL,NULL,'2026-07-07 02:03:00.933750',NULL,1),(56,3,35,128,NULL,NULL,1,'2026-07-07 02:03:00.933750',NULL,1),(57,3,36,129,NULL,NULL,1,'2026-07-07 02:03:03.876753',NULL,1),(58,3,36,130,NULL,NULL,1,'2026-07-07 02:03:03.876753',NULL,1),(59,5,30,106,NULL,NULL,1,'2026-07-07 02:05:42.459356',NULL,1),(60,5,30,107,NULL,NULL,1,'2026-07-07 02:05:42.459356',NULL,1),(61,5,30,108,NULL,NULL,1,'2026-07-07 02:05:42.459356',NULL,1),(62,5,31,109,NULL,NULL,1,'2026-07-07 02:05:48.990587',NULL,1),(63,5,31,110,1.230000000000000000000000000000,NULL,NULL,'2026-07-07 02:05:48.990588',NULL,1),(64,5,31,111,45.670000000000000000000000000000,NULL,NULL,'2026-07-07 02:05:48.990588',NULL,1),(65,5,32,112,NULL,NULL,1,'2026-07-07 02:05:56.318654',NULL,0),(66,5,32,113,NULL,NULL,1,'2026-07-07 02:05:56.318654',NULL,0),(67,5,32,114,45.670000000000000000000000000000,NULL,NULL,'2026-07-07 02:05:56.318654',NULL,0),(68,6,30,106,NULL,NULL,1,'2026-07-07 02:06:15.689975',NULL,1),(69,6,30,107,NULL,NULL,1,'2026-07-07 02:06:15.689976',NULL,1),(70,6,30,108,NULL,NULL,1,'2026-07-07 02:06:15.689976',NULL,1),(71,6,31,109,NULL,NULL,1,'2026-07-07 02:06:24.694249',NULL,0),(72,6,31,110,1.230000000000000000000000000000,NULL,NULL,'2026-07-07 02:06:24.694249',NULL,0),(73,6,31,111,45.670000000000000000000000000000,NULL,NULL,'2026-07-07 02:06:24.694249',NULL,0),(74,7,30,106,NULL,NULL,1,'2026-07-07 02:07:53.270822',NULL,1),(75,7,30,107,NULL,NULL,1,'2026-07-07 02:07:53.270823',NULL,1),(76,7,30,108,NULL,NULL,1,'2026-07-07 02:07:53.270823',NULL,1),(77,7,31,109,NULL,NULL,1,'2026-07-07 02:07:57.596192',NULL,0),(78,7,31,110,1.230000000000000000000000000000,NULL,NULL,'2026-07-07 02:07:57.596192',NULL,0),(79,7,31,111,45.670000000000000000000000000000,NULL,NULL,'2026-07-07 02:07:57.596192',NULL,0),(80,7,32,112,NULL,NULL,1,'2026-07-07 02:08:04.023788',NULL,0),(81,7,32,113,NULL,NULL,1,'2026-07-07 02:08:04.023788',NULL,0),(82,7,32,114,45.670000000000000000000000000000,NULL,NULL,'2026-07-07 02:08:04.023788',NULL,0),(83,8,30,106,NULL,NULL,1,'2026-07-07 02:09:10.835466',NULL,1),(84,8,30,107,NULL,NULL,1,'2026-07-07 02:09:10.835466',NULL,1),(85,8,30,108,NULL,NULL,1,'2026-07-07 02:09:10.835466',NULL,1),(86,8,31,109,NULL,NULL,1,'2026-07-07 02:09:17.187123',NULL,0),(87,8,31,110,1.230000000000000000000000000000,NULL,NULL,'2026-07-07 02:09:17.187123',NULL,0),(88,8,31,111,45.670000000000000000000000000000,NULL,NULL,'2026-07-07 02:09:17.187123',NULL,0),(89,9,30,106,NULL,NULL,1,'2026-07-07 02:13:27.862775',NULL,0),(90,9,30,107,NULL,NULL,1,'2026-07-07 02:13:27.862775',NULL,0),(91,9,30,108,NULL,NULL,1,'2026-07-07 02:13:27.862775',NULL,0),(92,10,30,106,NULL,NULL,1,'2026-07-07 02:17:10.614421',NULL,1),(93,10,30,107,NULL,NULL,1,'2026-07-07 02:17:10.614421',NULL,1),(94,10,30,108,NULL,NULL,1,'2026-07-07 02:17:10.614422',NULL,1),(95,10,31,109,NULL,NULL,1,'2026-07-07 02:17:18.590634',NULL,1),(96,10,31,110,1.230000000000000000000000000000,NULL,NULL,'2026-07-07 02:17:18.590635',NULL,1),(97,10,31,111,45.670000000000000000000000000000,NULL,NULL,'2026-07-07 02:17:18.590635',NULL,1),(98,10,32,112,NULL,NULL,1,'2026-07-07 02:17:23.191769',NULL,1),(99,10,32,113,NULL,NULL,1,'2026-07-07 02:17:23.191769',NULL,1),(100,10,32,114,45.670000000000000000000000000000,NULL,NULL,'2026-07-07 02:17:23.191770',NULL,1),(101,11,33,106,NULL,NULL,1,'2026-07-07 02:17:26.523095',NULL,1),(102,11,33,107,NULL,NULL,1,'2026-07-07 02:17:26.523095',NULL,1),(103,11,33,108,NULL,NULL,1,'2026-07-07 02:17:26.523094',NULL,1),(104,11,33,118,NULL,NULL,1,'2026-07-07 02:17:30.377135',NULL,1),(105,11,33,119,45.670000000000000000000000000000,NULL,NULL,'2026-07-07 02:17:30.377136',NULL,1),(106,11,33,120,NULL,NULL,1,'2026-07-07 02:17:30.377136',NULL,1),(107,11,34,121,NULL,NULL,0,'2026-07-07 02:17:54.827718',NULL,0),(108,11,34,122,0.000000000000000000000000000000,NULL,NULL,'2026-07-07 02:17:54.827718',NULL,0),(109,11,34,131,0.000000000000000000000000000000,NULL,NULL,'2026-07-07 02:17:54.827718',NULL,0),(110,11,34,132,0.000000000000000000000000000000,NULL,NULL,'2026-07-07 02:17:54.827718',NULL,0),(111,11,34,123,0.000000000000000000000000000000,NULL,NULL,'2026-07-07 02:17:54.827718',NULL,0),(112,11,34,133,0.000000000000000000000000000000,NULL,NULL,'2026-07-07 02:17:54.827718',NULL,0),(113,11,34,134,0.000000000000000000000000000000,NULL,NULL,'2026-07-07 02:17:54.827718',NULL,0),(114,11,34,124,0.000000000000000000000000000000,NULL,NULL,'2026-07-07 02:17:54.827718',NULL,0),(115,12,30,106,NULL,NULL,1,'2026-07-07 08:20:11.941140',NULL,1),(116,12,30,107,NULL,NULL,1,'2026-07-07 08:20:11.941141',NULL,1),(117,12,30,108,NULL,NULL,1,'2026-07-07 08:20:11.941142',NULL,1),(118,12,31,109,NULL,NULL,1,'2026-07-07 08:20:25.056179',NULL,1),(119,12,31,110,1.230000000000000000000000000000,NULL,NULL,'2026-07-07 08:20:25.056181',NULL,1),(120,12,31,111,45.670000000000000000000000000000,NULL,NULL,'2026-07-07 08:20:25.056182',NULL,1),(121,12,32,112,NULL,NULL,1,'2026-07-07 08:20:30.380874',NULL,0),(122,12,32,113,NULL,NULL,1,'2026-07-07 08:20:30.380876',NULL,0),(123,12,32,114,45.670000000000000000000000000000,NULL,NULL,'2026-07-07 08:20:30.380877',NULL,0),(124,13,30,106,NULL,NULL,1,'2026-07-07 08:23:06.401302',NULL,1),(125,13,30,107,NULL,NULL,1,'2026-07-07 08:23:06.401302',NULL,1),(126,13,30,108,NULL,NULL,1,'2026-07-07 08:23:06.401302',NULL,1),(127,14,30,106,NULL,NULL,1,'2026-07-07 08:25:19.204049',NULL,1),(128,14,30,107,NULL,NULL,1,'2026-07-07 08:25:19.204050',NULL,1),(129,14,30,108,NULL,NULL,1,'2026-07-07 08:25:19.204050',NULL,1),(130,15,30,106,NULL,NULL,1,'2026-07-07 08:28:13.381450',NULL,1),(131,15,30,107,NULL,NULL,1,'2026-07-07 08:28:13.381450',NULL,1),(132,15,30,108,NULL,NULL,1,'2026-07-07 08:28:13.381450',NULL,1),(133,15,31,109,NULL,NULL,1,'2026-07-07 08:33:01.934413',NULL,0),(134,15,31,110,1.230000000000000000000000000000,NULL,NULL,'2026-07-07 08:33:01.934414',NULL,0),(135,15,31,111,45.670000000000000000000000000000,NULL,NULL,'2026-07-07 08:33:01.934415',NULL,0),(136,16,30,106,NULL,NULL,1,'2026-07-07 08:33:11.403018',NULL,1),(137,16,30,107,NULL,NULL,1,'2026-07-07 08:33:11.403019',NULL,1),(138,16,30,108,NULL,NULL,1,'2026-07-07 08:33:11.403019',NULL,1),(139,17,30,106,NULL,NULL,1,'2026-07-07 08:33:34.979872',NULL,1),(140,17,30,107,NULL,NULL,1,'2026-07-07 08:33:34.979873',NULL,1),(141,17,30,108,NULL,NULL,1,'2026-07-07 08:33:34.979873',NULL,1),(142,18,30,106,NULL,NULL,1,'2026-07-07 08:35:40.680727',NULL,1),(143,18,30,107,NULL,NULL,1,'2026-07-07 08:35:40.680728',NULL,1),(144,18,30,108,NULL,NULL,1,'2026-07-07 08:35:40.680728',NULL,1),(145,19,30,106,NULL,NULL,1,'2026-07-07 08:36:34.923199',NULL,1),(146,19,30,107,NULL,NULL,1,'2026-07-07 08:36:34.923200',NULL,1),(147,19,30,108,NULL,NULL,1,'2026-07-07 08:36:34.923200',NULL,1),(148,20,30,106,NULL,NULL,1,'2026-07-07 08:49:28.853410',NULL,1),(149,20,30,107,NULL,NULL,1,'2026-07-07 08:49:28.853411',NULL,1),(150,20,30,108,NULL,NULL,1,'2026-07-07 08:49:28.853411',NULL,1),(151,21,30,106,NULL,NULL,1,'2026-07-07 08:49:53.290170',NULL,1),(152,21,30,107,NULL,NULL,1,'2026-07-07 08:49:53.290171',NULL,1),(153,21,30,108,NULL,NULL,1,'2026-07-07 08:49:53.290172',NULL,1),(154,22,30,106,NULL,NULL,1,'2026-07-07 08:52:41.692675',NULL,1),(155,22,30,107,NULL,NULL,1,'2026-07-07 08:52:41.692676',NULL,1),(156,22,30,108,NULL,NULL,1,'2026-07-07 08:52:41.692676',NULL,1),(157,23,30,106,NULL,NULL,1,'2026-07-07 08:55:33.110844',NULL,1),(158,23,30,107,NULL,NULL,1,'2026-07-07 08:55:33.110845',NULL,1),(159,23,30,108,NULL,NULL,1,'2026-07-07 08:55:33.110845',NULL,1),(160,24,30,106,NULL,NULL,1,'2026-07-07 08:59:06.614772',NULL,1),(161,24,30,107,NULL,NULL,1,'2026-07-07 08:59:06.614772',NULL,1),(162,24,30,108,NULL,NULL,1,'2026-07-07 08:59:06.614772',NULL,1),(163,25,30,106,NULL,NULL,1,'2026-07-07 09:03:43.326886',NULL,1),(164,25,30,107,NULL,NULL,1,'2026-07-07 09:03:43.326888',NULL,1),(165,25,30,108,NULL,NULL,1,'2026-07-07 09:03:43.326888',NULL,1),(166,26,30,106,NULL,NULL,1,'2026-07-07 09:05:26.291950',NULL,1),(167,26,30,107,NULL,NULL,1,'2026-07-07 09:05:26.291951',NULL,1),(168,26,30,108,NULL,NULL,1,'2026-07-07 09:05:26.291951',NULL,1),(169,27,30,106,NULL,NULL,1,'2026-07-07 09:07:20.405206',NULL,1),(170,27,30,107,NULL,NULL,1,'2026-07-07 09:07:20.405208',NULL,1),(171,27,30,108,NULL,NULL,1,'2026-07-07 09:07:20.405208',NULL,1),(172,28,30,106,NULL,NULL,1,'2026-07-07 09:10:41.119369',NULL,1),(173,28,30,107,NULL,NULL,1,'2026-07-07 09:10:41.119370',NULL,1),(174,28,30,108,NULL,NULL,1,'2026-07-07 09:10:41.119370',NULL,1),(175,29,30,106,NULL,NULL,1,'2026-07-07 09:12:57.911103',NULL,1),(176,29,30,107,NULL,NULL,1,'2026-07-07 09:12:57.911105',NULL,1),(177,29,30,108,NULL,NULL,1,'2026-07-07 09:12:57.911106',NULL,1),(178,30,30,106,NULL,NULL,1,'2026-07-07 09:25:46.186795',NULL,1),(179,30,30,107,NULL,NULL,1,'2026-07-07 09:25:46.186796',NULL,1),(180,30,30,108,NULL,NULL,1,'2026-07-07 09:25:46.186796',NULL,1),(181,31,30,106,NULL,NULL,1,'2026-07-07 09:27:45.336069',NULL,1),(182,31,30,107,NULL,NULL,1,'2026-07-07 09:27:45.336070',NULL,1),(183,31,30,108,NULL,NULL,1,'2026-07-07 09:27:45.336071',NULL,1),(184,32,30,106,NULL,NULL,1,'2026-07-07 09:29:33.396046',NULL,1),(185,32,30,107,NULL,NULL,1,'2026-07-07 09:29:33.396048',NULL,1),(186,32,30,108,NULL,NULL,1,'2026-07-07 09:29:33.396049',NULL,1),(187,33,30,106,NULL,NULL,1,'2026-07-07 09:36:18.389542',NULL,1),(188,33,30,107,NULL,NULL,1,'2026-07-07 09:36:18.389544',NULL,1),(189,33,30,108,NULL,NULL,1,'2026-07-07 09:36:18.389544',NULL,1),(190,34,30,106,NULL,NULL,1,'2026-07-07 09:36:41.114722',NULL,1),(191,34,30,107,NULL,NULL,1,'2026-07-07 09:36:41.114724',NULL,1),(192,34,30,108,NULL,NULL,1,'2026-07-07 09:36:41.114725',NULL,1);
/*!40000 ALTER TABLE `process_log_details` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `process_logs`
--

DROP TABLE IF EXISTS `process_logs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `process_logs` (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `serial_number_id` int NOT NULL,
  `is_active` tinyint(1) NOT NULL DEFAULT '1',
  `created_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime(6) DEFAULT NULL,
  `status` tinyint(1) NOT NULL DEFAULT '0',
  `is_finished` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`id`),
  KEY `ix_process_logs_serial_number_id` (`serial_number_id`),
  CONSTRAINT `fk_process_logs_serial_numbers_serial_number_id` FOREIGN KEY (`serial_number_id`) REFERENCES `serial_numbers` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=35 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `process_logs`
--

LOCK TABLES `process_logs` WRITE;
/*!40000 ALTER TABLE `process_logs` DISABLE KEYS */;
INSERT INTO `process_logs` VALUES (1,1,1,'2026-07-06 18:53:54','2026-07-07 01:54:23.336063',1,1),(2,2,1,'2026-07-06 18:54:06','2026-07-07 01:54:13.225347',1,0),(3,3,1,'2026-07-06 19:02:27','2026-07-07 02:03:03.876754',1,1),(4,4,1,'2026-07-06 19:02:44','2026-07-07 02:02:54.530944',1,0),(5,5,1,'2026-07-06 19:05:42','2026-07-07 02:05:56.318654',0,1),(6,6,1,'2026-07-06 19:06:16','2026-07-07 02:06:24.694250',0,1),(7,7,1,'2026-07-06 19:07:53','2026-07-07 02:08:04.023789',0,1),(8,8,1,'2026-07-06 19:09:11','2026-07-07 02:09:17.187124',0,1),(9,9,1,'2026-07-06 19:13:28',NULL,1,0),(10,10,1,'2026-07-06 19:17:11','2026-07-07 02:17:23.191770',1,0),(11,11,1,'2026-07-06 19:17:27','2026-07-07 02:17:54.827719',0,1),(12,12,1,'2026-07-07 01:20:12','2026-07-07 08:20:30.380880',0,1),(13,13,1,'2026-07-07 01:23:06',NULL,1,0),(14,14,1,'2026-07-07 01:25:19',NULL,1,0),(15,15,1,'2026-07-07 01:28:13','2026-07-07 08:33:01.934574',0,1),(16,16,1,'2026-07-07 01:33:11',NULL,1,0),(17,17,1,'2026-07-07 01:33:35',NULL,1,0),(18,18,1,'2026-07-07 01:35:41',NULL,1,0),(19,19,1,'2026-07-07 01:36:35',NULL,1,0),(20,20,1,'2026-07-07 01:49:29',NULL,1,0),(21,21,1,'2026-07-07 01:49:53',NULL,1,0),(22,22,1,'2026-07-07 01:52:42',NULL,1,0),(23,23,1,'2026-07-07 01:55:33',NULL,1,0),(24,24,1,'2026-07-07 01:59:07',NULL,1,0),(25,25,1,'2026-07-07 02:03:43',NULL,1,0),(26,26,1,'2026-07-07 02:05:26',NULL,1,0),(27,27,1,'2026-07-07 02:07:20',NULL,1,0),(28,28,1,'2026-07-07 02:10:41',NULL,1,0),(29,29,1,'2026-07-07 02:12:58',NULL,1,0),(30,30,1,'2026-07-07 02:25:46',NULL,1,0),(31,31,1,'2026-07-07 02:27:45',NULL,1,0),(32,32,1,'2026-07-07 02:29:33',NULL,1,0),(33,33,1,'2026-07-07 02:36:18',NULL,1,0),(34,34,1,'2026-07-07 02:36:41',NULL,1,0);
/*!40000 ALTER TABLE `process_logs` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `process_parameters`
--

DROP TABLE IF EXISTS `process_parameters`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `process_parameters` (
  `process_id` int NOT NULL,
  `parameter_id` int NOT NULL,
  PRIMARY KEY (`process_id`,`parameter_id`),
  KEY `ix_process_parameters_parameter_id` (`parameter_id`),
  CONSTRAINT `fk_process_parameters_parameters_parameter_id` FOREIGN KEY (`parameter_id`) REFERENCES `parameters` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_process_parameters_processes_process_id` FOREIGN KEY (`process_id`) REFERENCES `processes` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `process_parameters`
--

LOCK TABLES `process_parameters` WRITE;
/*!40000 ALTER TABLE `process_parameters` DISABLE KEYS */;
INSERT INTO `process_parameters` VALUES (30,106),(30,107),(30,108),(31,109),(31,110),(31,111),(32,112),(32,113),(32,114),(33,115),(33,116),(33,117),(33,118),(33,119),(33,120),(34,121),(34,122),(34,123),(34,124),(35,125),(35,126),(35,127),(35,128),(36,129),(36,130),(34,131),(34,132),(34,133),(34,134);
/*!40000 ALTER TABLE `process_parameters` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `processes`
--

DROP TABLE IF EXISTS `processes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `processes` (
  `id` int NOT NULL AUTO_INCREMENT,
  `code` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `description` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL DEFAULT '1',
  `created_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime(6) DEFAULT NULL,
  `order` int DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=64 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `processes`
--

LOCK TABLES `processes` WRITE;
/*!40000 ALTER TABLE `processes` DISABLE KEYS */;
INSERT INTO `processes` VALUES (30,'CLINCHING_SHORT_SIDE','CLINCHING SHORT SIDE','Process for clinching the short side of radiator.',1,'2026-06-08 02:50:46','2026-07-01 02:08:58.892750',1),(31,'CLINCHING_LONG_SIDE','Clincing long side','Process for clinching the long side of radiator.',1,'2026-06-08 02:50:46',NULL,2),(32,'HE_LEAK','He Leak','Helium leak testing process.',1,'2026-06-08 02:50:46',NULL,3),(33,'M_FAN_ASSY','M Fan Assy','Main fan assembly process.',1,'2026-06-08 02:50:46',NULL,4),(34,'M_FAN_INSPECTION','M Fan Characteristics Inspection','Inspection of main fan operational characteristics.',1,'2026-06-08 02:50:46',NULL,5),(35,'ECM_ASSY','Ecm Assy','Electronic Control Module assembly process.',1,'2026-06-08 02:50:46',NULL,6),(36,'FINAL_INSPECTION','Final Inspection','Final quality gate and inspection.',1,'2026-06-08 02:50:46',NULL,7);
/*!40000 ALTER TABLE `processes` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `refresh_tokens`
--

DROP TABLE IF EXISTS `refresh_tokens`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `refresh_tokens` (
  `id` int NOT NULL AUTO_INCREMENT,
  `token` varchar(512) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `expires_at` datetime(6) NOT NULL,
  `is_revoked` tinyint(1) NOT NULL,
  `revoked_at` datetime(6) DEFAULT NULL,
  `user_id` int NOT NULL,
  `created_at` datetime(6) NOT NULL,
  `updated_at` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ix_refresh_tokens_token` (`token`),
  KEY `ix_refresh_tokens_user_id` (`user_id`),
  CONSTRAINT `fk_refresh_tokens_users_user_id` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=124 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `refresh_tokens`
--

LOCK TABLES `refresh_tokens` WRITE;
/*!40000 ALTER TABLE `refresh_tokens` DISABLE KEYS */;
INSERT INTO `refresh_tokens` VALUES (1,'UcCDphyhCK+nydjap6FtdwgazuB84SE59bfFIUTQDuNHMFlMM+9tQUevkBlJhQJekYXwBeXH2hOE6hwuOyu6Ag==','2026-07-03 02:50:58.299794',0,NULL,1,'2026-06-26 02:50:58.360819',NULL),(102,'ifwFtlaZFlLCCFfE2F3TnlJGdV7NuyvPFwem/D9pkzXuioqltlXc15ebfaCB/m7xiTAFrjca98jC3CDFu+i/gg==','2026-07-03 03:50:16.552759',0,NULL,1,'2026-06-26 03:50:16.553557',NULL),(103,'hb35/qi6zrqglMVh7+jf96fpc7QFjda2RwU8WlW1QOTs5g6QSTPxDvTAZCur8UQHVyCK5Ajnmplz1JTp0vLMcw==','2026-07-06 06:13:02.225541',0,NULL,1,'2026-06-29 06:13:02.324993',NULL),(104,'yWdy/GEVvQVfWQIa9PynhVhCziWL4xfbWPCHwPqBb9rFOHCbfSG75hROIvjS/3ZxsOBUfwnMYmnF8scEUg+VOA==','2026-07-06 07:17:23.468800',0,NULL,1,'2026-06-29 07:17:23.546330',NULL),(105,'3Lt2b1SQSYJilCh/dd91+HqMZIATeenP6c04fyYPJdCSHxqbj9Oqi9dlynKPqAQ3HGkBLlmTiMi2sogOy8h1XA==','2026-07-06 08:37:21.540277',0,NULL,1,'2026-06-29 08:37:21.651642',NULL),(106,'uNHpdtfaVRlPwZ3mQ79qaz7sg7l8P5QKEx/EpDvnNjOCX4FXFs8Kqenx6Aghjmn2tiAcZW0HMcyfCMWSsfCLVA==','2026-07-06 11:42:54.205878',0,NULL,1,'2026-06-29 11:42:54.431067',NULL),(107,'T87oA2WkCKjV5eVx+ifteU4DwoYto39KbPuUmo0vkQAJ4Dv+3NKydRjjs6K9PBPm+LsaO4dGONZwlf6LKd6SEA==','2026-07-07 01:16:26.681023',0,NULL,1,'2026-06-30 01:16:26.839869',NULL),(108,'XzJNA8xdMZH++5qG8lFB2PiKA5JU34RlpW9ft44ZIdE1wE13qXbqGwQhXmg+C+b7gpGWqXsdRmX90sf5tdsU8A==','2026-07-08 01:48:56.612987',0,NULL,1,'2026-07-01 01:48:56.789720',NULL),(109,'7RspypnH3Y6PgzYq3mV/NKNLw4bZPfH8gQ3xaGf316fo1GF0JaNvPeLvTtd/WbdDVaVXwFOLycFxcY3QFS1+xQ==','2026-07-08 06:25:50.844749',0,NULL,1,'2026-07-01 06:25:50.888125',NULL),(110,'JnRPx2gBf+cziilTQfcWOpLTEbbW8iFBvxB88NyD7pfHV7DXnbKqf3xcSNuPUVc2YtJaPzRd3ofRaLgftA9rKw==','2026-07-08 07:26:55.738485',0,NULL,1,'2026-07-01 07:26:56.025122',NULL),(111,'PPLNcpzIL+I2+GBd4bCZ/o4w6YG8F5DDKoF+X+UTGfiZe5VAu6DW8nSBkL3DnhklbQs3f7kFNP99czrH2Y/apg==','2026-07-09 01:38:56.066456',0,NULL,1,'2026-07-02 01:38:56.220613',NULL),(112,'RuY4SlCao0wbbyTTTjjL6N8QgNjn1r7JHqzp1lj6CMYy4bW8UOg1Yz4oznNjB5FUPlDlg7GSADQAjorgQQaKGA==','2026-07-09 02:16:41.094854',0,NULL,102,'2026-07-02 02:16:41.132399',NULL),(113,'/PpGozhbLib1ES9RULHXm9hZ9xnbN1AUhT4lUMtiaWVugV3cXvxJ8XIidm1whyR4xgGy1cb5qyqEoJmZbbgWGw==','2026-07-09 04:00:41.922614',0,NULL,1,'2026-07-02 04:00:41.988228',NULL),(114,'2JbVHTrtEuHfh84KtP9DST6rY9gaMquJgW3ApdJqs6JJc2FnG9JPBYjjA9o/BHOvQKgaosEhSDIZI6sRDNo5Bg==','2026-07-09 06:01:55.972292',0,NULL,1,'2026-07-02 06:01:56.103344',NULL),(115,'iO5LVxp+qSz3Sr+Az8Lqhv4pbitB0omo9v32ALZCJ7yxlo/OSiENhBhXQolM0LYp/cUqH07G7cEd5Tkna8jUGQ==','2026-07-09 07:30:49.415644',0,NULL,1,'2026-07-02 07:30:49.499446',NULL),(116,'T2ZgWP9xz8jUKtkVSNkaGotrN7fSXJnkkFRnNqvytImFl8AFkrJ2NfJqjDEc2dbfv/VAumsRg8lm6XDbr+3eVA==','2026-07-10 02:06:52.814194',0,NULL,1,'2026-07-03 02:06:52.989146',NULL),(117,'6KZX7MfgWk8//McbxrcweyQVePlubtUt5zs6TZjFI+X33MlJFdQuNdA8CCE+PrciIdNrg5Co+LyTH+t4BdShyQ==','2026-07-10 08:41:39.639925',0,NULL,1,'2026-07-03 08:41:39.813027',NULL),(118,'1aL5AaLzRKK5VK2N+hJq8f+Uo2LNqXyJ7xJ5fvpqyD2+em94YQngVYT10NV5fa2QE7Unp1EuLs9T15zamQOpZQ==','2026-07-14 01:27:30.421505',0,NULL,1,'2026-07-07 01:27:30.574932',NULL),(119,'dY0mmobkAbuwl7+4IOo/fY7+OqqK1guliDCNh2NWFK9DykJQreGki7D0h80043xSLKicgPJidx3bysUmnEowbQ==','2026-07-14 02:47:30.609700',0,NULL,1,'2026-07-07 02:47:30.740338',NULL),(120,'P+PYmgTXteK3WCsR236aYA7zI/xsq8AsuDTTUdhTMdWVez2DxzW3mPRookXzKE5l0j6qgWkCw/qXULTydpGDiw==','2026-07-14 03:28:22.183081',0,NULL,1,'2026-07-07 03:28:22.355623',NULL),(121,'iFHPSIBm4wLmP1B+ENI7bM3i/qkE/P4+6kDaK1uvUK+SEO7doySLrJV+ET8ty3pSkKKUdrfigZBq8HcF0Q2Rtw==','2026-07-14 07:49:58.603921',0,NULL,1,'2026-07-07 07:49:58.920776',NULL),(122,'jkpi27gmgzu2LO5JpdhZZxftyO3+jzJAjyeeTDiitiXabbQoEJwwBC0/X+L5ktmBMtOroJg8pN9U1xt+C08yFQ==','2026-07-15 03:57:06.547647',0,NULL,1,'2026-07-08 03:57:06.686126',NULL),(123,'5sYdmGlVFdhidGC2o0iK1FvGfOYYfj2VeNAjc50X7+zuYCt+KruFq3I3nDX7Zwb5MJzmmeMAL8nmBOK32BBeZA==','2026-07-15 04:37:38.185311',0,NULL,1,'2026-07-08 04:37:38.337996',NULL);
/*!40000 ALTER TABLE `refresh_tokens` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `serial_number_issues`
--

DROP TABLE IF EXISTS `serial_number_issues`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `serial_number_issues` (
  `id` int NOT NULL AUTO_INCREMENT,
  `serial_number_id` int NOT NULL,
  `issue_id` int NOT NULL,
  `created_at` datetime(6) NOT NULL,
  `created_by` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ix_serial_number_issues_serial_number_id_issue_id` (`serial_number_id`,`issue_id`),
  KEY `ix_serial_number_issues_issue_id` (`issue_id`),
  CONSTRAINT `fk_serial_number_issues_issues_issue_id` FOREIGN KEY (`issue_id`) REFERENCES `issues` (`id`) ON DELETE RESTRICT,
  CONSTRAINT `fk_serial_number_issues_serial_numbers_serial_number_id` FOREIGN KEY (`serial_number_id`) REFERENCES `serial_numbers` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=103 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `serial_number_issues`
--

LOCK TABLES `serial_number_issues` WRITE;
/*!40000 ALTER TABLE `serial_number_issues` DISABLE KEYS */;
INSERT INTO `serial_number_issues` VALUES (1,1,32,'2026-07-07 01:53:54.313715','OP001'),(2,1,33,'2026-07-07 01:53:54.316416','OP001'),(3,1,34,'2026-07-07 01:53:54.316949','OP001'),(4,2,35,'2026-07-07 01:54:05.677243','OP001'),(5,2,36,'2026-07-07 01:54:05.677244','OP001'),(6,2,39,'2026-07-07 01:54:05.677244','OP001'),(7,3,32,'2026-07-07 02:02:25.528041','OP001'),(8,3,33,'2026-07-07 02:02:25.530925','OP001'),(9,3,34,'2026-07-07 02:02:25.531860','OP001'),(10,4,35,'2026-07-07 02:02:43.650152','OP001'),(11,4,36,'2026-07-07 02:02:43.650152','OP001'),(12,4,39,'2026-07-07 02:02:43.650152','OP001'),(13,5,32,'2026-07-07 02:05:42.380616','OP001'),(14,5,33,'2026-07-07 02:05:42.381263','OP001'),(15,5,34,'2026-07-07 02:05:42.381703','OP001'),(16,6,32,'2026-07-07 02:06:15.609484','OP001'),(17,6,33,'2026-07-07 02:06:15.610013','OP001'),(18,6,34,'2026-07-07 02:06:15.610610','OP001'),(19,7,32,'2026-07-07 02:07:53.195619','OP001'),(20,7,33,'2026-07-07 02:07:53.195842','OP001'),(21,7,34,'2026-07-07 02:07:53.195983','OP001'),(22,8,32,'2026-07-07 02:09:10.756968','OP001'),(23,8,33,'2026-07-07 02:09:10.757453','OP001'),(24,8,34,'2026-07-07 02:09:10.757802','OP001'),(25,9,32,'2026-07-07 02:13:27.781670','OP001'),(26,9,33,'2026-07-07 02:13:27.782599','OP001'),(27,9,34,'2026-07-07 02:13:27.783142','OP001'),(28,10,32,'2026-07-07 02:17:10.533226','OP001'),(29,10,33,'2026-07-07 02:17:10.533562','OP001'),(30,10,34,'2026-07-07 02:17:10.533819','OP001'),(31,11,35,'2026-07-07 02:17:26.448482','OP001'),(32,11,36,'2026-07-07 02:17:26.448482','OP001'),(33,11,39,'2026-07-07 02:17:26.448482','OP001'),(34,12,32,'2026-07-07 08:20:10.548803','OP001'),(35,12,33,'2026-07-07 08:20:10.550906','OP001'),(36,12,34,'2026-07-07 08:20:10.551319','OP001'),(37,13,32,'2026-07-07 08:23:05.794367','OP001'),(38,13,33,'2026-07-07 08:23:05.795277','OP001'),(39,13,34,'2026-07-07 08:23:05.795725','OP001'),(40,14,32,'2026-07-07 08:25:18.516775','OP001'),(41,14,33,'2026-07-07 08:25:18.518141','OP001'),(42,14,34,'2026-07-07 08:25:18.518540','OP001'),(43,15,32,'2026-07-07 08:28:12.891194','OP001'),(44,15,33,'2026-07-07 08:28:12.892449','OP001'),(45,15,34,'2026-07-07 08:28:12.892735','OP001'),(46,16,32,'2026-07-07 08:33:11.120758','OP001'),(47,16,33,'2026-07-07 08:33:11.121298','OP001'),(48,16,34,'2026-07-07 08:33:11.121631','OP001'),(49,17,32,'2026-07-07 08:33:34.853857','OP001'),(50,17,33,'2026-07-07 08:33:34.854343','OP001'),(51,17,34,'2026-07-07 08:33:34.854821','OP001'),(52,18,32,'2026-07-07 08:35:40.603660','OP001'),(53,18,33,'2026-07-07 08:35:40.604129','OP001'),(54,18,34,'2026-07-07 08:35:40.604527','OP001'),(55,19,32,'2026-07-07 08:36:34.830955','OP001'),(56,19,33,'2026-07-07 08:36:34.831817','OP001'),(57,19,34,'2026-07-07 08:36:34.832416','OP001'),(58,20,32,'2026-07-07 08:49:28.202598','OP001'),(59,20,33,'2026-07-07 08:49:28.203755','OP001'),(60,20,34,'2026-07-07 08:49:28.204111','OP001'),(61,21,32,'2026-07-07 08:49:53.157336','OP001'),(62,21,33,'2026-07-07 08:49:53.157949','OP001'),(63,21,34,'2026-07-07 08:49:53.158309','OP001'),(64,22,32,'2026-07-07 08:52:41.006492','OP001'),(65,22,33,'2026-07-07 08:52:41.007910','OP001'),(66,22,34,'2026-07-07 08:52:41.008367','OP001'),(67,23,32,'2026-07-07 08:55:32.362191','OP001'),(68,23,33,'2026-07-07 08:55:32.364124','OP001'),(69,23,34,'2026-07-07 08:55:32.364672','OP001'),(70,24,32,'2026-07-07 08:59:05.942564','OP001'),(71,24,33,'2026-07-07 08:59:05.943942','OP001'),(72,24,34,'2026-07-07 08:59:05.944225','OP001'),(73,25,32,'2026-07-07 09:03:41.886356','OP001'),(74,25,33,'2026-07-07 09:03:41.888072','OP001'),(75,25,34,'2026-07-07 09:03:41.889122','OP001'),(76,26,32,'2026-07-07 09:05:25.740862','OP001'),(77,26,33,'2026-07-07 09:05:25.741512','OP001'),(78,26,34,'2026-07-07 09:05:25.741899','OP001'),(79,27,32,'2026-07-07 09:07:18.913347','OP001'),(80,27,33,'2026-07-07 09:07:18.914994','OP001'),(81,27,34,'2026-07-07 09:07:18.916235','OP001'),(82,28,32,'2026-07-07 09:10:40.568466','OP001'),(83,28,33,'2026-07-07 09:10:40.569170','OP001'),(84,28,34,'2026-07-07 09:10:40.569507','OP001'),(85,29,32,'2026-07-07 09:12:56.500053','OP001'),(86,29,33,'2026-07-07 09:12:56.501525','OP001'),(87,29,34,'2026-07-07 09:12:56.502402','OP001'),(88,30,32,'2026-07-07 09:25:45.669427','OP001'),(89,30,33,'2026-07-07 09:25:45.670612','OP001'),(90,30,34,'2026-07-07 09:25:45.671023','OP001'),(91,31,32,'2026-07-07 09:27:43.393111','OP001'),(92,31,33,'2026-07-07 09:27:43.395195','OP001'),(93,31,34,'2026-07-07 09:27:43.397607','OP001'),(94,32,32,'2026-07-07 09:29:31.413681','OP001'),(95,32,33,'2026-07-07 09:29:31.416943','OP001'),(96,32,34,'2026-07-07 09:29:31.418506','OP001'),(97,33,32,'2026-07-07 09:36:16.492303','OP001'),(98,33,33,'2026-07-07 09:36:16.495328','OP001'),(99,33,34,'2026-07-07 09:36:16.496462','OP001'),(100,34,32,'2026-07-07 09:36:40.896942','OP001'),(101,34,33,'2026-07-07 09:36:40.898150','OP001'),(102,34,34,'2026-07-07 09:36:40.899182','OP001');
/*!40000 ALTER TABLE `serial_number_issues` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `serial_number_relations`
--

DROP TABLE IF EXISTS `serial_number_relations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `serial_number_relations` (
  `id` int NOT NULL AUTO_INCREMENT,
  `parent_serial_number_id` int NOT NULL,
  `child_serial_number_id` int NOT NULL,
  `created_at` datetime(6) NOT NULL,
  `created_by` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ix_serial_number_relations_parent_serial_number_id_child_serial~` (`parent_serial_number_id`,`child_serial_number_id`),
  KEY `ix_serial_number_relations_child_serial_number_id` (`child_serial_number_id`),
  CONSTRAINT `fk_serial_number_relations_serial_numbers_child_serial_number_id` FOREIGN KEY (`child_serial_number_id`) REFERENCES `serial_numbers` (`id`) ON DELETE RESTRICT,
  CONSTRAINT `fk_serial_number_relations_serial_numbers_parent_serial_number_~` FOREIGN KEY (`parent_serial_number_id`) REFERENCES `serial_numbers` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `serial_number_relations`
--

LOCK TABLES `serial_number_relations` WRITE;
/*!40000 ALTER TABLE `serial_number_relations` DISABLE KEYS */;
INSERT INTO `serial_number_relations` VALUES (1,1,2,'2026-07-07 01:54:05.704638','OP001'),(2,3,4,'2026-07-07 02:02:43.682424','OP001'),(3,10,11,'2026-07-07 02:17:26.469858','OP001');
/*!40000 ALTER TABLE `serial_number_relations` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `serial_numbers`
--

DROP TABLE IF EXISTS `serial_numbers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `serial_numbers` (
  `id` int NOT NULL AUTO_INCREMENT,
  `serial_number_code` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `type` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `created_at` datetime(6) NOT NULL,
  `created_by` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `updated_at` datetime(6) DEFAULT NULL,
  `updated_by` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ix_serial_numbers_serial_number_code` (`serial_number_code`)
) ENGINE=InnoDB AUTO_INCREMENT=35 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `serial_numbers`
--

LOCK TABLES `serial_numbers` WRITE;
/*!40000 ALTER TABLE `serial_numbers` DISABLE KEYS */;
INSERT INTO `serial_numbers` VALUES (1,'CC20260707001','CLINCHING','2026-07-07 01:53:54.312600','OP001','2026-07-07 01:54:05.704984',NULL),(2,'MF20260707001','MFanAssy','2026-07-07 01:54:05.673281','OP001',NULL,NULL),(3,'CC20260707002','CLINCHING','2026-07-07 02:02:25.519636','OP001','2026-07-07 02:02:43.709836',NULL),(4,'MF20260707002','MFanAssy','2026-07-07 02:02:43.629317','OP001',NULL,NULL),(5,'CC20260707003','CLINCHING','2026-07-07 02:05:42.378162','OP001',NULL,NULL),(6,'CC20260707004','CLINCHING','2026-07-07 02:06:15.608223','OP001',NULL,NULL),(7,'CC20260707005','CLINCHING','2026-07-07 02:07:53.195063','OP001',NULL,NULL),(8,'CC20260707006','CLINCHING','2026-07-07 02:09:10.755421','OP001',NULL,NULL),(9,'CC20260707007','CLINCHING','2026-07-07 02:13:27.780395','OP001',NULL,NULL),(10,'CC20260707008','CLINCHING','2026-07-07 02:17:10.531856','OP001','2026-07-07 02:17:26.470081',NULL),(11,'MF20260707003','MFanAssy','2026-07-07 02:17:26.444713','OP001',NULL,NULL),(12,'CC20260707009','CLINCHING','2026-07-07 08:20:10.539980','OP001',NULL,NULL),(13,'CC20260707010','CLINCHING','2026-07-07 08:23:05.790404','OP001',NULL,NULL),(14,'CC20260707011','CLINCHING','2026-07-07 08:25:18.510520','OP001',NULL,NULL),(15,'CC20260707012','CLINCHING','2026-07-07 08:28:12.887809','OP001',NULL,NULL),(16,'CC20260707013','CLINCHING','2026-07-07 08:33:11.116484','OP001',NULL,NULL),(17,'CC20260707014','CLINCHING','2026-07-07 08:33:34.852240','OP001',NULL,NULL),(18,'CC20260707015','CLINCHING','2026-07-07 08:35:40.601907','OP001',NULL,NULL),(19,'CC20260707016','CLINCHING','2026-07-07 08:36:34.828891','OP001',NULL,NULL),(20,'CC20260707017','CLINCHING','2026-07-07 08:49:28.197922','OP001',NULL,NULL),(21,'CC20260707018','CLINCHING','2026-07-07 08:49:53.155798','OP001',NULL,NULL),(22,'CC20260707019','CLINCHING','2026-07-07 08:52:41.000507','OP001',NULL,NULL),(23,'CC20260707020','CLINCHING','2026-07-07 08:55:32.357914','OP001',NULL,NULL),(24,'CC20260707021','CLINCHING','2026-07-07 08:59:05.939709','OP001',NULL,NULL),(25,'CC20260707022','CLINCHING','2026-07-07 09:03:41.876906','OP001',NULL,NULL),(26,'CC20260707023','CLINCHING','2026-07-07 09:05:25.737662','OP001',NULL,NULL),(27,'CC20260707024','CLINCHING','2026-07-07 09:07:18.904417','OP001',NULL,NULL),(28,'CC20260707025','CLINCHING','2026-07-07 09:10:40.565544','OP001',NULL,NULL),(29,'CC20260707026','CLINCHING','2026-07-07 09:12:56.490236','OP001',NULL,NULL),(30,'CC20260707027','CLINCHING','2026-07-07 09:25:45.664847','OP001',NULL,NULL),(31,'CC20260707028','CLINCHING','2026-07-07 09:27:43.375314','OP001',NULL,NULL),(32,'CC20260707029','CLINCHING','2026-07-07 09:29:31.391120','OP001',NULL,NULL),(33,'CC20260707030','CLINCHING','2026-07-07 09:36:16.480361','OP001',NULL,NULL),(34,'CC20260707031','CLINCHING','2026-07-07 09:36:40.893080','OP001',NULL,NULL);
/*!40000 ALTER TABLE `serial_numbers` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `stock_in_reworks`
--

DROP TABLE IF EXISTS `stock_in_reworks`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `stock_in_reworks` (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `serial_number_id` int NOT NULL,
  `issue_number_before` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `issue_number_after` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `qty` int NOT NULL,
  `note` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `status` tinyint(1) NOT NULL,
  `created_at` datetime(6) NOT NULL,
  `updated_at` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `ix_stock_in_reworks_serial_number_id` (`serial_number_id`),
  CONSTRAINT `fk_stock_in_reworks_serial_numbers_serial_number_id` FOREIGN KEY (`serial_number_id`) REFERENCES `serial_numbers` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `stock_in_reworks`
--

LOCK TABLES `stock_in_reworks` WRITE;
/*!40000 ALTER TABLE `stock_in_reworks` DISABLE KEYS */;
/*!40000 ALTER TABLE `stock_in_reworks` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `stock_ins`
--

DROP TABLE IF EXISTS `stock_ins`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `stock_ins` (
  `id` int NOT NULL AUTO_INCREMENT,
  `code` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `part_id` int NOT NULL,
  `supply_qty` int NOT NULL,
  `supply_date` datetime(6) NOT NULL,
  `receipt_qty` int NOT NULL,
  `receipt_date` datetime(6) NOT NULL,
  `created_at` datetime(6) NOT NULL,
  `updated_at` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ix_stock_ins_code` (`code`),
  KEY `ix_stock_ins_part_id` (`part_id`),
  CONSTRAINT `fk_stock_ins_parts_part_id` FOREIGN KEY (`part_id`) REFERENCES `parts` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=40 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `stock_ins`
--

LOCK TABLES `stock_ins` WRITE;
/*!40000 ALTER TABLE `stock_ins` DISABLE KEYS */;
INSERT INTO `stock_ins` VALUES (32,'ST20260702001',1,100,'2026-07-02 01:39:03.141000',52,'2026-07-02 01:39:03.141000','2026-07-02 01:39:15.623288','2026-07-07 09:36:40.928324'),(33,'ST20260702002',2,50,'2026-07-02 01:39:26.591000',2,'2026-07-02 01:39:26.591000','2026-07-02 01:39:36.796021','2026-07-07 09:36:40.957956'),(34,'ST20260702003',3,70,'2026-07-02 01:39:40.498000',23,'2026-07-02 01:39:40.498000','2026-07-02 01:39:49.026788','2026-07-07 09:36:41.026411'),(35,'ST20260702004',8,40,'2026-07-02 01:39:52.322000',30,'2026-07-02 01:39:52.322000','2026-07-02 01:40:01.265893','2026-07-07 02:17:26.456848'),(36,'ST20260702005',9,220,'2026-07-02 01:40:02.788000',210,'2026-07-02 01:40:02.788000','2026-07-02 01:40:14.235753','2026-07-07 02:17:26.462098'),(39,'ST20260702006',10,220,'2026-07-02 01:43:49.702000',211,'2026-07-02 01:43:49.702000','2026-07-02 01:44:02.587216','2026-07-07 02:17:26.467195');
/*!40000 ALTER TABLE `stock_ins` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `users` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `username` varchar(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `password_hash` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `role` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `is_active` tinyint(1) NOT NULL,
  `created_at` datetime(6) NOT NULL,
  `updated_at` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ix_users_username` (`username`)
) ENGINE=InnoDB AUTO_INCREMENT=104 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `users`
--

LOCK TABLES `users` WRITE;
/*!40000 ALTER TABLE `users` DISABLE KEYS */;
INSERT INTO `users` VALUES (1,'Admin','admin','$2a$11$.EYJp.I0B9pD0yHWpE.0oOLFuuaYTMZwY15C7Z8F19X5lzZRsLncm','admin',1,'2026-06-26 02:50:58.144807',NULL),(102,'User','user','$2a$11$.FyPOKB/SOWN4JAO.PjDyekHXyK3by1CTOXUSuUgLybLg6D7FVdTC','user',1,'2026-07-02 02:16:01.895207',NULL),(103,'Operator','op001','$2a$11$g6dB6U.VDbeQ5ndtsLuQse57vZ8eV0manaEPC4dsZ/QKJ90v03gDe','guest',1,'2026-07-02 04:02:43.593910',NULL);
/*!40000 ALTER TABLE `users` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Dumping routines for database 'trss_traceability_system'
--
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-07-08 15:48:22
