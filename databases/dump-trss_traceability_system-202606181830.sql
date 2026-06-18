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
INSERT INTO `__efmigrationshistory` VALUES ('20260505094745_InitialCreate','9.0.0'),('20260505095151_LowerCaseNaming','9.0.0'),('20260506024707_CreatePartTable','9.0.0'),('20260506042901_AddIsActiveToPart','9.0.0'),('20260506063113_AddProcessesTable','9.0.0'),('20260506064558_AddParametersTable','9.0.0'),('20260506072120_AddProcessParametersTable','9.0.0'),('20260506090232_AddStockInsAndIssuesTable','9.0.0'),('20260506092039_AddPrintersTable','9.0.0'),('20260506094549_AddAppConfigEntity','9.0.0'),('20260508064522_AddProcessLogsAndParameterDataType','9.0.0'),('20260508070234_AddIsActiveToProcessLog','9.0.0'),('20260508073745_AllowMultipleValuesPerParameter','9.0.0'),('20260612025348_AddMqttPrintRequests','9.0.0');
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
INSERT INTO `app_configs` VALUES (1,'PRINTER_NAME_STOCK_IN','Printer-Line-10',NULL,'2026-05-08 08:00:05.122765','2026-06-03 04:28:48.157000'),(2,'PRINTER_NAME_LINE_1','Printer-Line-01','Printer name for Line 1 production.','2026-05-08 08:00:05.122764',NULL),(3,'PRINTER_NAME_LINE_2','Printer-Line-02','Printer name for Line 2 production.','2026-05-08 08:00:05.122764',NULL);
/*!40000 ALTER TABLE `app_configs` ENABLE KEYS */;
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
) ENGINE=InnoDB AUTO_INCREMENT=35 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `issues`
--

LOCK TABLES `issues` WRITE;
/*!40000 ALTER TABLE `issues` DISABLE KEYS */;
INSERT INTO `issues` VALUES (1,'ISS-00030',30,'2026-05-08 08:00:05.213045',NULL),(2,'ISS-00029',29,'2026-05-08 08:00:05.213045',NULL),(3,'ISS-00028',28,'2026-05-08 08:00:05.213045',NULL),(4,'ISS-00027',27,'2026-05-08 08:00:05.213045',NULL),(5,'ISS-00026',26,'2026-05-08 08:00:05.213045',NULL),(6,'ISS-00025',25,'2026-05-08 08:00:05.213045',NULL),(7,'ISS-00024',24,'2026-05-08 08:00:05.213045',NULL),(8,'ISS-00023',23,'2026-05-08 08:00:05.213045',NULL),(9,'ISS-00022',22,'2026-05-08 08:00:05.213045',NULL),(10,'ISS-00021',21,'2026-05-08 08:00:05.213045',NULL),(11,'ISS-00020',20,'2026-05-08 08:00:05.213045',NULL),(12,'ISS-00019',19,'2026-05-08 08:00:05.213044',NULL),(13,'ISS-00018',18,'2026-05-08 08:00:05.213044',NULL),(14,'ISS-00017',17,'2026-05-08 08:00:05.213044',NULL),(15,'ISS-00016',16,'2026-05-08 08:00:05.213044',NULL),(16,'ISS-00015',15,'2026-05-08 08:00:05.213044',NULL),(17,'ISS-00014',14,'2026-05-08 08:00:05.213044',NULL),(18,'ISS-00013',13,'2026-05-08 08:00:05.213043',NULL),(19,'ISS-00012',12,'2026-05-08 08:00:05.213043',NULL),(20,'ISS-00011',11,'2026-05-08 08:00:05.213043',NULL),(21,'ISS-00010',10,'2026-05-08 08:00:05.213043',NULL),(22,'ISS-00009',9,'2026-05-08 08:00:05.213043',NULL),(23,'ISS-00008',8,'2026-05-08 08:00:05.213043',NULL),(24,'ISS-00007',7,'2026-05-08 08:00:05.213043',NULL),(25,'ISS-00006',6,'2026-05-08 08:00:05.213043',NULL),(26,'ISS-00005',5,'2026-05-08 08:00:05.213043',NULL),(27,'ISS-00004',4,'2026-05-08 08:00:05.213043',NULL),(28,'ISS-00003',3,'2026-05-08 08:00:05.213043',NULL),(29,'ISS-00002',2,'2026-05-08 08:00:05.213043',NULL),(30,'ISS-00001',1,'2026-05-08 08:00:05.213042',NULL),(31,'20260602001',31,'2026-06-02 06:18:02.036234',NULL),(32,'20260603001',32,'2026-06-03 04:37:14.445486',NULL),(34,'20260605001',34,'2026-06-05 03:15:38.702286',NULL);
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
) ENGINE=InnoDB AUTO_INCREMENT=93 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `mqtt_print_requests`
--

LOCK TABLES `mqtt_print_requests` WRITE;
/*!40000 ALTER TABLE `mqtt_print_requests` DISABLE KEYS */;
INSERT INTO `mqtt_print_requests` VALUES (1,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}','Pending',NULL,'2026-06-12 02:56:27.238422',NULL,NULL),(2,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}','Pending',NULL,'2026-06-12 02:56:59.746892',NULL,NULL),(3,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}','Pending',NULL,'2026-06-12 03:05:23.384494',NULL,NULL),(4,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}','Pending',NULL,'2026-06-12 03:05:23.645441',NULL,NULL),(5,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}','Pending',NULL,'2026-06-12 03:05:24.063965',NULL,NULL),(6,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}','Pending',NULL,'2026-06-12 03:05:24.468072',NULL,NULL),(7,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}','Pending',NULL,'2026-06-12 03:05:24.822984',NULL,NULL),(8,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}','Pending',NULL,'2026-06-12 03:05:25.208123',NULL,NULL),(9,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}','Pending',NULL,'2026-06-12 03:05:25.812180',NULL,NULL),(10,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}','Pending',NULL,'2026-06-12 03:05:26.019535',NULL,NULL),(11,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}','Pending',NULL,'2026-06-12 03:05:26.214759',NULL,NULL),(12,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}','Pending',NULL,'2026-06-12 03:05:26.618405',NULL,NULL),(13,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}','Pending',NULL,'2026-06-12 03:05:27.029882',NULL,NULL),(14,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}','Pending',NULL,'2026-06-12 03:05:27.385476',NULL,NULL),(15,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}','Pending',NULL,'2026-06-12 03:05:27.755745',NULL,NULL),(16,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}','Pending',NULL,'2026-06-12 03:05:27.958738',NULL,NULL),(17,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}','Pending',NULL,'2026-06-12 03:05:28.166595',NULL,NULL),(18,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}','Pending',NULL,'2026-06-12 03:05:28.526282',NULL,NULL),(19,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}','Pending',NULL,'2026-06-12 03:05:28.736765',NULL,NULL),(20,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}','Pending',NULL,'2026-06-12 03:05:29.126920',NULL,NULL),(21,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}','Pending',NULL,'2026-06-12 03:05:29.486315',NULL,NULL),(22,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}','Pending',NULL,'2026-06-12 03:05:29.708953',NULL,NULL),(23,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}','Pending',NULL,'2026-06-12 03:05:30.374930',NULL,NULL),(24,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}','Pending',NULL,'2026-06-12 03:05:30.746194',NULL,NULL),(25,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}','Pending',NULL,'2026-06-12 03:05:30.952340',NULL,NULL),(26,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}\n','Pending',NULL,'2026-06-12 06:33:42.337432',NULL,NULL),(27,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}\n','Pending',NULL,'2026-06-12 06:33:47.730886',NULL,NULL),(28,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}\n','Pending',NULL,'2026-06-12 06:33:48.720959',NULL,NULL),(29,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}\n','Pending',NULL,'2026-06-12 06:33:49.653064',NULL,NULL),(30,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}\n','Pending',NULL,'2026-06-12 06:33:50.345732',NULL,NULL),(31,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}\n','Pending',NULL,'2026-06-12 06:33:50.871273',NULL,NULL),(32,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}\n','Pending',NULL,'2026-06-12 06:33:51.275403',NULL,NULL),(33,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}\n','Pending',NULL,'2026-06-12 06:33:51.472436',NULL,NULL),(34,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}\n','Pending',NULL,'2026-06-12 06:33:51.803387',NULL,NULL),(35,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}\n','Pending',NULL,'2026-06-12 06:33:52.235824',NULL,NULL),(36,'M_FAN_ASSY','ISS-001','{\n  \"process_code\": \"M_FAN_ASSY\",\n  \"issue_number\": \"ISS-001\"\n}\n','Pending',NULL,'2026-06-12 06:33:52.595177',NULL,NULL),(37,'CLINCHING_SHORT_SIDE','ISS-001','\n{\n  \"process_code\": \"CLINCHING_SHORT_SIDE\",\n  \"issue_number\": \"ISS-001\"\n}','Pending',NULL,'2026-06-12 06:33:55.966289',NULL,NULL),(38,'CLINCHING_SHORT_SIDE','ISS-001','\n{\n  \"process_code\": \"CLINCHING_SHORT_SIDE\",\n  \"issue_number\": \"ISS-001\"\n}','Pending',NULL,'2026-06-12 06:33:57.397455',NULL,NULL),(39,'CLINCHING_SHORT_SIDE','ISS-001','\n{\n  \"process_code\": \"CLINCHING_SHORT_SIDE\",\n  \"issue_number\": \"ISS-001\"\n}','Pending',NULL,'2026-06-12 06:33:58.011635',NULL,NULL),(40,'CLINCHING_SHORT_SIDE','ISS-001','\n{\n  \"process_code\": \"CLINCHING_SHORT_SIDE\",\n  \"issue_number\": \"ISS-001\"\n}','Pending',NULL,'2026-06-12 06:33:58.415131',NULL,NULL),(41,'CLINCHING_SHORT_SIDE','ISS-001','\n{\n  \"process_code\": \"CLINCHING_SHORT_SIDE\",\n  \"issue_number\": \"ISS-001\"\n}','Pending',NULL,'2026-06-12 06:33:58.736062',NULL,NULL),(42,'CLINCHING_SHORT_SIDE','ISS-002','\n{\n  \"process_code\": \"CLINCHING_SHORT_SIDE\",\n  \"issue_number\": \"ISS-002\"\n}','Pending',NULL,'2026-06-12 06:34:55.317635',NULL,NULL),(43,'CLINCHING_SHORT_SIDE','ISS-002','\n{\n  \"process_code\": \"CLINCHING_SHORT_SIDE\",\n  \"issue_number\": \"ISS-002\"\n}','Pending',NULL,'2026-06-12 06:40:27.255869',NULL,NULL),(44,'CLINCHING_SHORT_SIDE','ISS-002','\n{\n  \"process_code\": \"CLINCHING_SHORT_SIDE\",\n  \"issue_number\": \"ISS-002\"\n}','Processed',NULL,'2026-06-12 06:43:10.378437','2026-06-12 06:43:10.668744','2026-06-12 06:43:10.669308'),(45,'CLINCHING_SHORT_SIDE','ISS-002','\n{\n  \"process_code\": \"CLINCHING_SHORT_SIDE\",\n  \"issue_number\": \"ISS-002\"\n}','Processed',NULL,'2026-06-12 06:43:51.522788','2026-06-12 06:43:51.805173','2026-06-12 06:43:51.806462'),(46,'CLINCHING_SHORT_SIDE','ISS-002','\n{\n  \"process_code\": \"CLINCHING_SHORT_SIDE\",\n  \"issue_number\": \"ISS-002\"\n}','Processed',NULL,'2026-06-12 06:43:57.242671','2026-06-12 06:47:53.720031','2026-06-12 06:47:53.720065'),(47,'CLINCHING_SHORT_SIDE','20260605001','\n{\n  \"process_code\": \"CLINCHING_SHORT_SIDE\",\n  \"issue_number\": \"20260605001\"\n}','Processed',NULL,'2026-06-12 06:59:28.783078','2026-06-12 06:59:29.506126','2026-06-12 06:59:29.509492'),(48,'CLINCHING_SHORT_SIDE','20260605001','\n{\n  \"process_code\": \"CLINCHING_SHORT_SIDE\",\n  \"issue_number\": \"20260605001\"\n}','Processed',NULL,'2026-06-12 06:59:51.339133','2026-06-12 06:59:51.376207','2026-06-12 06:59:51.376277'),(49,'CLINCHING_SHORT_SIDE','20260605001','\n{\n  \"process_code\": \"CLINCHING_SHORT_SIDE\",\n  \"issue_number\": \"20260605001\"\n}','Processed',NULL,'2026-06-12 07:04:04.783441','2026-06-12 07:04:04.835388','2026-06-12 07:04:04.835619'),(50,'CLINCHING_SHORT_SIDE','20260605001','\n{\n  \"process_code\": \"CLINCHING_SHORT_SIDE\",\n  \"issue_number\": \"20260605001\"\n}','Processed',NULL,'2026-06-12 07:14:17.302350','2026-06-12 07:14:17.353775','2026-06-12 07:14:17.354336'),(51,'CLINCHING_SHORT_SIDE','20260605001','\n{\n  \"process_code\": \"CLINCHING_SHORT_SIDE\",\n  \"issue_number\": \"20260605001\"\n}','Processed',NULL,'2026-06-12 07:16:45.491909','2026-06-12 07:16:45.538479','2026-06-12 07:16:45.538677'),(52,'CLINCHING_SHORT_SIDE','20260605001','\n{\n  \"process_code\": \"CLINCHING_SHORT_SIDE\",\n  \"issue_number\": \"20260605001\"\n}','Processed',NULL,'2026-06-12 07:17:15.949191','2026-06-12 07:17:16.067392','2026-06-12 07:17:16.067517'),(53,'CLINCHING_SHORT_SIDE','20260605001','\n{\n  \"issue_number\": \"20260605001\"\n}','Processed',NULL,'2026-06-12 07:27:16.672497','2026-06-12 07:27:17.379694','2026-06-12 07:27:17.385306'),(54,'M_FAN_ASSY','20260605001','\n{\n  \"issue_number\": \"20260605001\"\n}','Processed',NULL,'2026-06-12 07:27:36.276876','2026-06-12 07:27:36.325822','2026-06-12 07:27:36.325933'),(55,'CLINCHING_SHORT_SIDE','','{\n    \"d\": {\n    },\n    \"issue_number\": \"\",\n    \"ts\": \"2026-06-12T07:47:16.811892\"\n}','Processed',NULL,'2026-06-12 08:47:39.330151','2026-06-12 08:47:39.352367','2026-06-12 08:47:39.352477'),(56,'CLINCHING_SHORT_SIDE','','{\n    \"d\": {\n    },\n    \"issue_number\": \"\",\n    \"ts\": \"2026-06-12T07:47:17.761616\"\n}','Processed',NULL,'2026-06-12 08:47:40.261560','2026-06-12 08:47:40.270161','2026-06-12 08:47:40.270234'),(57,'CLINCHING_SHORT_SIDE','','{\n    \"d\": {\n    },\n    \"issue_number\": \"\",\n    \"ts\": \"2026-06-12T07:47:17.945615\"\n}','Processed',NULL,'2026-06-12 08:47:40.456329','2026-06-12 08:47:40.464758','2026-06-12 08:47:40.464824'),(58,'CLINCHING_SHORT_SIDE','','{\n    \"d\": {\n    },\n    \"issue_number\": \"\",\n    \"ts\": \"2026-06-12T07:47:18.128804\"\n}','Processed',NULL,'2026-06-12 08:47:40.629336','2026-06-12 08:47:40.637600','2026-06-12 08:47:40.637667'),(59,'CLINCHING_SHORT_SIDE','','{\n    \"d\": {\n    },\n    \"issue_number\": \"\",\n    \"ts\": \"2026-06-12T07:47:18.345986\"\n}','Processed',NULL,'2026-06-12 08:47:40.860468','2026-06-12 08:47:40.871780','2026-06-12 08:47:40.871857'),(60,'CLINCHING_SHORT_SIDE','','{\n    \"d\": {\n    },\n    \"issue_number\": \"\",\n    \"ts\": \"2026-06-12T07:47:18.527774\"\n}','Processed',NULL,'2026-06-12 08:47:41.032293','2026-06-12 08:47:41.040431','2026-06-12 08:47:41.040485'),(61,'CLINCHING_SHORT_SIDE','','{\n    \"d\": {\n    },\n    \"issue_number\": \"\",\n    \"ts\": \"2026-06-12T07:47:18.695820\"\n}','Processed',NULL,'2026-06-12 08:47:41.206956','2026-06-12 08:47:41.213364','2026-06-12 08:47:41.213648'),(62,'CLINCHING_SHORT_SIDE','','{\n    \"d\": {\n    },\n    \"issue_number\": \"\",\n    \"ts\": \"2026-06-12T07:47:56.327827\"\n}','Processed',NULL,'2026-06-12 08:48:18.836298','2026-06-12 08:48:18.846545','2026-06-12 08:48:18.846586'),(63,'CLINCHING_SHORT_SIDE','','{\n    \"d\": {\n    },\n    \"issue_number\": \"\",\n    \"ts\": \"2026-06-12T07:47:56.493508\"\n}','Processed',NULL,'2026-06-12 08:48:18.998253','2026-06-12 08:48:19.011440','2026-06-12 08:48:19.011506'),(64,'CLINCHING_SHORT_SIDE','','{\n    \"d\": {\n    },\n    \"issue_number\": \"\",\n    \"ts\": \"2026-06-12T07:47:56.693253\"\n}','Processed',NULL,'2026-06-12 08:48:19.188028','2026-06-12 08:48:19.200446','2026-06-12 08:48:19.200480'),(65,'CLINCHING_SHORT_SIDE','','{\n    \"d\": {\n    },\n    \"issue_number\": \"\",\n    \"ts\": \"2026-06-12T07:47:58.060144\"\n}','Processed',NULL,'2026-06-12 08:48:20.563581','2026-06-12 08:48:20.570032','2026-06-12 08:48:20.570069'),(66,'CLINCHING_SHORT_SIDE','','{\n    \"d\": {\n    },\n    \"issue_number\": \"\",\n    \"ts\": \"2026-06-12T07:47:58.993014\"\n}','Processed',NULL,'2026-06-12 08:48:21.490850','2026-06-12 08:48:21.497538','2026-06-12 08:48:21.497593'),(67,'CLINCHING_SHORT_SIDE','','{\n    \"d\": {\n    },\n    \"issue_number\": \"\",\n    \"ts\": \"2026-06-12T07:47:59.194330\"\n}','Processed',NULL,'2026-06-12 08:48:21.699694','2026-06-12 08:48:21.705020','2026-06-12 08:48:21.705186'),(68,'CLINCHING_SHORT_SIDE','','{\n    \"d\": {\n    },\n    \"issue_number\": \"\",\n    \"ts\": \"2026-06-12T07:48:00.010652\"\n}','Processed',NULL,'2026-06-12 08:48:22.516834','2026-06-12 08:48:22.525808','2026-06-12 08:48:22.525910'),(69,'CLINCHING_SHORT_SIDE','','{\n    \"d\": {\n    },\n    \"issue_number\": \"\",\n    \"ts\": \"2026-06-12T07:48:00.242787\"\n}','Processed',NULL,'2026-06-12 08:48:22.739839','2026-06-12 08:48:22.747743','2026-06-12 08:48:22.747795'),(70,'CLINCHING_SHORT_SIDE','','{\n    \"d\": {\n    },\n    \"issue_number\": \"\",\n    \"ts\": \"2026-06-12T07:48:00.826353\"\n}','Processed',NULL,'2026-06-12 08:48:23.333528','2026-06-12 08:48:23.344051','2026-06-12 08:48:23.344106'),(71,'CLINCHING_SHORT_SIDE','','{\n    \"d\": {\n    },\n    \"issue_number\": \"\",\n    \"ts\": \"2026-06-12T07:48:01.226820\"\n}','Processed',NULL,'2026-06-12 08:48:23.735668','2026-06-12 08:48:23.753079','2026-06-12 08:48:23.753169'),(72,'CLINCHING_SHORT_SIDE','','{\n    \"d\": {\n    },\n    \"issue_number\": \"\",\n    \"ts\": \"2026-06-12T07:48:01.443594\"\n}','Processed',NULL,'2026-06-12 08:48:23.941836','2026-06-12 08:48:23.952229','2026-06-12 08:48:23.952270'),(73,'M_FAN_ASSY','','{\n    \"d\": {\n    },\n    \"issue_number\": \"\",\n    \"ts\": \"2026-06-12T07:48:32.558808\"\n}','Processed',NULL,'2026-06-12 08:48:55.060583','2026-06-12 08:48:55.070551','2026-06-12 08:48:55.070592'),(74,'M_FAN_ASSY','','{\n    \"d\": {\n    },\n    \"issue_number\": \"\",\n    \"ts\": \"2026-06-12T07:48:32.708535\"\n}','Processed',NULL,'2026-06-12 08:48:55.202888','2026-06-12 08:48:55.209059','2026-06-12 08:48:55.209084'),(75,'M_FAN_ASSY','','{\n    \"d\": {\n    },\n    \"issue_number\": \"\",\n    \"ts\": \"2026-06-12T07:48:32.908643\"\n}','Processed',NULL,'2026-06-12 08:48:55.414059','2026-06-12 08:48:55.419812','2026-06-12 08:48:55.419845'),(76,'M_FAN_ASSY','','{\n    \"d\": {\n    },\n    \"issue_number\": \"\",\n    \"ts\": \"2026-06-12T07:48:33.441973\"\n}','Processed',NULL,'2026-06-12 08:48:55.939040','2026-06-12 08:48:55.943354','2026-06-12 08:48:55.943375'),(77,'M_FAN_ASSY','','{\n    \"d\": {\n    },\n    \"issue_number\": \"\",\n    \"ts\": \"2026-06-12T07:48:33.891929\"\n}','Processed',NULL,'2026-06-12 08:48:56.386693','2026-06-12 08:48:56.393326','2026-06-12 08:48:56.393374'),(78,'M_FAN_ASSY','','{\n    \"d\": {\n    },\n    \"issue_number\": \"\",\n    \"ts\": \"2026-06-12T07:48:34.158864\"\n}','Processed',NULL,'2026-06-12 08:48:56.658074','2026-06-12 08:48:56.661697','2026-06-12 08:48:56.661711'),(79,'M_FAN_ASSY','','{\n    \"d\": {\n    },\n    \"issue_number\": \"\",\n    \"ts\": \"2026-06-12T07:48:43.858066\"\n}','Processed',NULL,'2026-06-12 08:49:06.354799','2026-06-12 08:49:06.364117','2026-06-12 08:49:06.364160'),(80,'M_FAN_ASSY','','{\n    \"d\": {\n    },\n    \"issue_number\": \"\",\n    \"ts\": \"2026-06-12T07:48:45.424849\"\n}','Processed',NULL,'2026-06-12 08:49:07.924700','2026-06-12 08:49:07.933063','2026-06-12 08:49:07.933132'),(81,'M_FAN_ASSY','','{\n    \"d\": {\n    },\n    \"issue_number\": \"\",\n    \"ts\": \"2026-06-12T07:48:46.058226\"\n}','Processed',NULL,'2026-06-12 08:49:08.562743','2026-06-12 08:49:08.567670','2026-06-12 08:49:08.567695'),(82,'M_FAN_ASSY','','{\n    \"d\": {\n    },\n    \"issue_number\": \"\",\n    \"ts\": \"2026-06-12T07:49:01.574459\"\n}','Processed',NULL,'2026-06-12 08:49:24.085373','2026-06-12 08:49:24.095492','2026-06-12 08:49:24.095540'),(83,'CLINCHING_SHORT_SIDE','202606050001','{\n    \"d\": {\n    },\n    \"issue_number\": \"202606050001\",\n    \"ts\": \"2026-06-12T07:50:02.272341\"\n}','Processed',NULL,'2026-06-12 08:50:24.775733','2026-06-12 08:50:24.780920','2026-06-12 08:50:24.780946'),(84,'CLINCHING_SHORT_SIDE','202606050001','{\n    \"d\": {\n    },\n    \"issue_number\": \"202606050001\",\n    \"ts\": \"2026-06-12T07:50:02.889863\"\n}','Processed',NULL,'2026-06-12 08:50:25.393337','2026-06-12 08:50:25.396592','2026-06-12 08:50:25.396603'),(85,'CLINCHING_SHORT_SIDE','202606050001','{\n    \"d\": {\n    },\n    \"issue_number\": \"202606050001\",\n    \"ts\": \"2026-06-12T07:50:03.054781\"\n}','Processed',NULL,'2026-06-12 08:50:25.553746','2026-06-12 08:50:25.556929','2026-06-12 08:50:25.556973'),(86,'CLINCHING_SHORT_SIDE','202606050001','{\n    \"d\": {\n    },\n    \"issue_number\": \"202606050001\",\n    \"ts\": \"2026-06-12T07:50:03.338621\"\n}','Processed',NULL,'2026-06-12 08:50:25.842561','2026-06-12 08:50:25.848493','2026-06-12 08:50:25.848520'),(87,'CLINCHING_SHORT_SIDE','202606050001','{\n    \"d\": {\n    },\n    \"issue_number\": \"202606050001\",\n    \"ts\": \"2026-06-12T07:50:03.537847\"\n}','Processed',NULL,'2026-06-12 08:50:26.034405','2026-06-12 08:50:26.037363','2026-06-12 08:50:26.037376'),(88,'M_FAN_ASSY','06050001','{\n    \"d\": {\n    },\n    \"issue_number\": \"06050001\",\n    \"ts\": \"2026-06-12T07:50:08.287788\"\n}','Processed',NULL,'2026-06-12 08:50:30.786381','2026-06-12 08:50:30.792041','2026-06-12 08:50:30.792737'),(89,'M_FAN_ASSY','06050001','{\n    \"d\": {\n    },\n    \"issue_number\": \"06050001\",\n    \"ts\": \"2026-06-12T07:50:09.305723\"\n}','Processed',NULL,'2026-06-12 08:50:31.812458','2026-06-12 08:50:31.817005','2026-06-12 08:50:31.817025'),(90,'M_FAN_ASSY','06050001','{\n    \"d\": {\n    },\n    \"issue_number\": \"06050001\",\n    \"ts\": \"2026-06-12T07:50:09.504953\"\n}','Processed',NULL,'2026-06-12 08:50:32.002518','2026-06-12 08:50:32.005386','2026-06-12 08:50:32.005401'),(91,'CLINCHING_SHORT_SIDE','halo Agung','{\n    \"issue_number\": \"halo Agung\"\n}','Processed',NULL,'2026-06-12 08:53:48.557555','2026-06-12 08:53:48.564320','2026-06-12 08:53:48.564335'),(92,'M_FAN_ASSY','halo ugang','{\n    \"issue_number\": \"halo ugang\"\n}','Processed',NULL,'2026-06-12 08:53:49.133746','2026-06-12 08:53:49.140090','2026-06-12 08:53:49.140122');
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
  `is_active` tinyint(1) NOT NULL DEFAULT '1',
  `created_at` datetime(6) NOT NULL,
  `updated_at` datetime(6) DEFAULT NULL,
  `data_type` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL DEFAULT '',
  PRIMARY KEY (`id`),
  UNIQUE KEY `ix_parameters_code` (`code`)
) ENGINE=InnoDB AUTO_INCREMENT=131 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `parameters`
--

LOCK TABLES `parameters` WRITE;
/*!40000 ALTER TABLE `parameters` DISABLE KEYS */;
INSERT INTO `parameters` VALUES (106,'CORE_ASM_RESULT','Core Asm',NULL,1,'2026-06-08 09:50:46.372805',NULL,'boolean'),(107,'UPPER_TANK_ASM_RESULT','Upper Tank Asm Result',NULL,1,'2026-06-08 09:50:46.372851',NULL,'boolean'),(108,'LOWER_TANK_ASM_RESULT','Lower Tank Asm Result',NULL,1,'2026-06-08 09:50:46.372861',NULL,'boolean'),(109,'CLINCHING_HEIGHT_RESULT','Clinching Height Result',NULL,1,'2026-06-08 09:50:46.372870',NULL,'boolean'),(110,'CLINCHING_HEIGHT_VALUE','Clinching Height Value',NULL,1,'2026-06-08 09:50:46.372872',NULL,'number'),(111,'END_PLATE_WIDTH_VALUE','End Plate Width Value',NULL,1,'2026-06-08 09:50:46.372874',NULL,'number'),(112,'CAP_TYPE_POSITION_RESULT','Cap Type & Position',NULL,1,'2026-06-08 09:50:46.372878',NULL,'boolean'),(113,'LEAK_TEST_RESULT','Leak Test Result',NULL,1,'2026-06-08 09:50:46.372880',NULL,'boolean'),(114,'LEAK_VALUE','Leak Value',NULL,1,'2026-06-08 09:50:46.372882',NULL,'number'),(115,'FAN_ASM_RESULT','Fan Asm Result',NULL,1,'2026-06-08 09:50:46.372885',NULL,'boolean'),(116,'MOTOR_ASM_RESULT','Motor Asm Result',NULL,1,'2026-06-08 09:50:46.372888',NULL,'boolean'),(117,'FUN_GUIDE_ASM_RESULT','Fun Guide Asm Result',NULL,1,'2026-06-08 09:50:46.372889',NULL,'boolean'),(118,'BOLT_TIGHTEN_RESULT','Bolt tighten result',NULL,1,'2026-06-08 09:50:46.372890',NULL,'boolean'),(119,'BOLT_TIGHTEN_VALUE','Bold Tighten Value',NULL,1,'2026-06-08 09:50:46.372892',NULL,'number'),(120,'NUT_TIGHTEN_RESULT','Nut Tighten Result',NULL,1,'2026-06-08 09:50:46.372894',NULL,'boolean'),(121,'M_FAN_TEST_RESULT','M Fan Test Result',NULL,1,'2026-06-08 09:50:46.372897',NULL,'boolean'),(122,'M_FAN_INSPECTION_ROTATION_SPEED_VALUE','M Fan Inspection Rotation Speed Value',NULL,1,'2026-06-08 09:50:46.372899',NULL,'number'),(123,'M_FAN_INSPECTION_AMPERE_VALUE','M Fan Inspection Amperage Value',NULL,1,'2026-06-08 09:50:46.372900',NULL,'number'),(124,'M_FAN_INSPECTION_WIND_DIRECTION_VALUE','M Fan Inspection Wind Direction Value',NULL,1,'2026-06-08 09:50:46.372903',NULL,'number'),(125,'RAD_CORE_ASM_NAME_LABEL_RESULT','Rad Core Asm Name Label Result',NULL,1,'2026-06-08 09:50:46.372916',NULL,'boolean'),(126,'MOTOR_FAN_ASSY_LABEL_RESULT','Motor Fan Assy Label Result',NULL,1,'2026-06-08 09:50:46.372917',NULL,'boolean'),(127,'ECM_ASSY_BOLT_TIGHTEN_VALUE','ECM Assy Bolt Tighten Result',NULL,1,'2026-06-08 09:50:46.372919',NULL,'number'),(128,'ECM_ASSY_BOLT_TIGHTEN_RESULT','ECM Assy Nut Tighten Result',NULL,1,'2026-06-08 09:50:46.372920',NULL,'boolean'),(129,'FINAL_INSPECTION_RAD_CORE_ASM_NAME_LABEL_RESULT','Final Inspection Rad Core Asm Name Label Result',NULL,1,'2026-06-08 09:50:46.372925',NULL,'boolean'),(130,'ALL_CHECK_POINT_RESULT','All Check Point Result',NULL,1,'2026-06-08 09:50:46.372926',NULL,'boolean');
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
  `created_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime(6) DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=101 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `parts`
--

LOCK TABLES `parts` WRITE;
/*!40000 ALTER TABLE `parts` DISABLE KEYS */;
INSERT INTO `parts` VALUES (1,'PN-0001','Sample Part 1','This is an auto-generated sample description for part 1.','2026-05-08 08:00:05',NULL,1),(2,'PN-0002','Sample Part 2','This is an auto-generated sample description for part 2.','2026-05-08 08:00:05',NULL,1),(3,'PN-0003','Sample Part 3','This is an auto-generated sample description for part 3.','2026-05-08 08:00:05',NULL,1),(4,'PN-0004','Sample Part 4','This is an auto-generated sample description for part 4.','2026-05-08 08:00:05',NULL,1),(5,'PN-0005','Sample Part 5','This is an auto-generated sample description for part 5.','2026-05-08 08:00:05',NULL,1),(6,'PN-0006','Sample Part 6','This is an auto-generated sample description for part 6.','2026-05-08 08:00:05',NULL,1),(7,'PN-0007','Sample Part 7','This is an auto-generated sample description for part 7.','2026-05-08 08:00:05',NULL,1),(8,'PN-0008','Sample Part 8','This is an auto-generated sample description for part 8.','2026-05-08 08:00:05',NULL,1),(9,'PN-0009','Sample Part 9','This is an auto-generated sample description for part 9.','2026-05-08 08:00:05',NULL,1),(10,'PN-0010','Sample Part 10','This is an auto-generated sample description for part 10.','2026-05-08 08:00:05',NULL,1),(11,'PN-0011','Sample Part 11','This is an auto-generated sample description for part 11.','2026-05-08 08:00:05',NULL,1),(12,'PN-0012','Sample Part 12','This is an auto-generated sample description for part 12.','2026-05-08 08:00:05',NULL,1),(13,'PN-0013','Sample Part 13','This is an auto-generated sample description for part 13.','2026-05-08 08:00:05',NULL,1),(14,'PN-0014','Sample Part 14','This is an auto-generated sample description for part 14.','2026-05-08 08:00:05',NULL,1),(15,'PN-0015','Sample Part 15','This is an auto-generated sample description for part 15.','2026-05-08 08:00:05',NULL,1),(16,'PN-0016','Sample Part 16','This is an auto-generated sample description for part 16.','2026-05-08 08:00:05',NULL,1),(17,'PN-0017','Sample Part 17','This is an auto-generated sample description for part 17.','2026-05-08 08:00:05',NULL,1),(18,'PN-0018','Sample Part 18','This is an auto-generated sample description for part 18.','2026-05-08 08:00:05',NULL,1),(19,'PN-0019','Sample Part 19','This is an auto-generated sample description for part 19.','2026-05-08 08:00:05',NULL,1),(20,'PN-0020','Sample Part 20','This is an auto-generated sample description for part 20.','2026-05-08 08:00:05',NULL,1),(21,'PN-0021','Sample Part 21','This is an auto-generated sample description for part 21.','2026-05-08 08:00:05',NULL,1),(22,'PN-0022','Sample Part 22','This is an auto-generated sample description for part 22.','2026-05-08 08:00:05',NULL,1),(23,'PN-0023','Sample Part 23','This is an auto-generated sample description for part 23.','2026-05-08 08:00:05',NULL,1),(24,'PN-0024','Sample Part 24','This is an auto-generated sample description for part 24.','2026-05-08 08:00:05',NULL,1),(25,'PN-0025','Sample Part 25','This is an auto-generated sample description for part 25.','2026-05-08 08:00:05',NULL,1),(26,'PN-0026','Sample Part 26','This is an auto-generated sample description for part 26.','2026-05-08 08:00:05',NULL,1),(27,'PN-0027','Sample Part 27','This is an auto-generated sample description for part 27.','2026-05-08 08:00:05',NULL,1),(28,'PN-0028','Sample Part 28','This is an auto-generated sample description for part 28.','2026-05-08 08:00:05',NULL,1),(29,'PN-0029','Sample Part 29','This is an auto-generated sample description for part 29.','2026-05-08 08:00:05',NULL,1),(30,'PN-0030','Sample Part 30','This is an auto-generated sample description for part 30.','2026-05-08 08:00:05',NULL,1),(31,'PN-0031','Sample Part 31','This is an auto-generated sample description for part 31.','2026-05-08 08:00:05',NULL,1),(32,'PN-0032','Sample Part 32','This is an auto-generated sample description for part 32.','2026-05-08 08:00:05',NULL,1),(33,'PN-0033','Sample Part 33','This is an auto-generated sample description for part 33.','2026-05-08 08:00:05',NULL,1),(34,'PN-0034','Sample Part 34','This is an auto-generated sample description for part 34.','2026-05-08 08:00:05',NULL,1),(35,'PN-0035','Sample Part 35','This is an auto-generated sample description for part 35.','2026-05-08 08:00:05',NULL,1),(36,'PN-0036','Sample Part 36','This is an auto-generated sample description for part 36.','2026-05-08 08:00:05',NULL,1),(37,'PN-0037','Sample Part 37','This is an auto-generated sample description for part 37.','2026-05-08 08:00:05',NULL,1),(38,'PN-0038','Sample Part 38','This is an auto-generated sample description for part 38.','2026-05-08 08:00:05',NULL,1),(39,'PN-0039','Sample Part 39','This is an auto-generated sample description for part 39.','2026-05-08 08:00:05',NULL,1),(40,'PN-0040','Sample Part 40','This is an auto-generated sample description for part 40.','2026-05-08 08:00:05',NULL,1),(41,'PN-0041','Sample Part 41','This is an auto-generated sample description for part 41.','2026-05-08 08:00:05',NULL,1),(42,'PN-0042','Sample Part 42','This is an auto-generated sample description for part 42.','2026-05-08 08:00:05',NULL,1),(43,'PN-0043','Sample Part 43','This is an auto-generated sample description for part 43.','2026-05-08 08:00:05',NULL,1),(44,'PN-0044','Sample Part 44','This is an auto-generated sample description for part 44.','2026-05-08 08:00:05',NULL,1),(45,'PN-0045','Sample Part 45','This is an auto-generated sample description for part 45.','2026-05-08 08:00:05',NULL,1),(46,'PN-0046','Sample Part 46','This is an auto-generated sample description for part 46.','2026-05-08 08:00:05',NULL,1),(47,'PN-0047','Sample Part 47','This is an auto-generated sample description for part 47.','2026-05-08 08:00:05',NULL,1),(48,'PN-0048','Sample Part 48','This is an auto-generated sample description for part 48.','2026-05-08 08:00:05',NULL,1),(49,'PN-0049','Sample Part 49','This is an auto-generated sample description for part 49.','2026-05-08 08:00:05',NULL,1),(50,'PN-0050','Sample Part 50','This is an auto-generated sample description for part 50.','2026-05-08 08:00:05',NULL,1),(51,'PN-0051','Sample Part 51','This is an auto-generated sample description for part 51.','2026-05-08 08:00:05',NULL,1),(52,'PN-0052','Sample Part 52','This is an auto-generated sample description for part 52.','2026-05-08 08:00:05',NULL,1),(53,'PN-0053','Sample Part 53','This is an auto-generated sample description for part 53.','2026-05-08 08:00:05',NULL,1),(54,'PN-0054','Sample Part 54','This is an auto-generated sample description for part 54.','2026-05-08 08:00:05',NULL,1),(55,'PN-0055','Sample Part 55','This is an auto-generated sample description for part 55.','2026-05-08 08:00:05',NULL,1),(56,'PN-0056','Sample Part 56','This is an auto-generated sample description for part 56.','2026-05-08 08:00:05',NULL,1),(57,'PN-0057','Sample Part 57','This is an auto-generated sample description for part 57.','2026-05-08 08:00:05',NULL,1),(58,'PN-0058','Sample Part 58','This is an auto-generated sample description for part 58.','2026-05-08 08:00:05',NULL,1),(59,'PN-0059','Sample Part 59','This is an auto-generated sample description for part 59.','2026-05-08 08:00:05',NULL,1),(60,'PN-0060','Sample Part 60','This is an auto-generated sample description for part 60.','2026-05-08 08:00:05',NULL,1),(61,'PN-0061','Sample Part 61','This is an auto-generated sample description for part 61.','2026-05-08 08:00:05',NULL,1),(62,'PN-0062','Sample Part 62','This is an auto-generated sample description for part 62.','2026-05-08 08:00:05',NULL,1),(63,'PN-0063','Sample Part 63','This is an auto-generated sample description for part 63.','2026-05-08 08:00:05',NULL,1),(64,'PN-0064','Sample Part 64','This is an auto-generated sample description for part 64.','2026-05-08 08:00:05',NULL,1),(65,'PN-0065','Sample Part 65','This is an auto-generated sample description for part 65.','2026-05-08 08:00:05',NULL,1),(66,'PN-0066','Sample Part 66','This is an auto-generated sample description for part 66.','2026-05-08 08:00:05',NULL,1),(67,'PN-0067','Sample Part 67','This is an auto-generated sample description for part 67.','2026-05-08 08:00:05',NULL,1),(68,'PN-0068','Sample Part 68','This is an auto-generated sample description for part 68.','2026-05-08 08:00:05',NULL,1),(69,'PN-0069','Sample Part 69','This is an auto-generated sample description for part 69.','2026-05-08 08:00:05',NULL,1),(70,'PN-0070','Sample Part 70','This is an auto-generated sample description for part 70.','2026-05-08 08:00:05',NULL,1),(71,'PN-0071','Sample Part 71','This is an auto-generated sample description for part 71.','2026-05-08 08:00:05',NULL,1),(72,'PN-0072','Sample Part 72','This is an auto-generated sample description for part 72.','2026-05-08 08:00:05',NULL,1),(73,'PN-0073','Sample Part 73','This is an auto-generated sample description for part 73.','2026-05-08 08:00:05',NULL,1),(74,'PN-0074','Sample Part 74','This is an auto-generated sample description for part 74.','2026-05-08 08:00:05',NULL,1),(75,'PN-0075','Sample Part 75','This is an auto-generated sample description for part 75.','2026-05-08 08:00:05',NULL,1),(76,'PN-0076','Sample Part 76','This is an auto-generated sample description for part 76.','2026-05-08 08:00:05',NULL,1),(77,'PN-0077','Sample Part 77','This is an auto-generated sample description for part 77.','2026-05-08 08:00:05',NULL,1),(78,'PN-0078','Sample Part 78','This is an auto-generated sample description for part 78.','2026-05-08 08:00:05',NULL,1),(79,'PN-0079','Sample Part 79','This is an auto-generated sample description for part 79.','2026-05-08 08:00:05',NULL,1),(80,'PN-0080','Sample Part 80','This is an auto-generated sample description for part 80.','2026-05-08 08:00:05',NULL,1),(81,'PN-0081','Sample Part 81','This is an auto-generated sample description for part 81.','2026-05-08 08:00:05',NULL,1),(82,'PN-0082','Sample Part 82','This is an auto-generated sample description for part 82.','2026-05-08 08:00:05',NULL,1),(83,'PN-0083','Sample Part 83','This is an auto-generated sample description for part 83.','2026-05-08 08:00:05',NULL,1),(84,'PN-0084','Sample Part 84','This is an auto-generated sample description for part 84.','2026-05-08 08:00:05',NULL,1),(85,'PN-0085','Sample Part 85','This is an auto-generated sample description for part 85.','2026-05-08 08:00:05',NULL,1),(86,'PN-0086','Sample Part 86','This is an auto-generated sample description for part 86.','2026-05-08 08:00:05',NULL,1),(87,'PN-0087','Sample Part 87','This is an auto-generated sample description for part 87.','2026-05-08 08:00:05',NULL,1),(88,'PN-0088','Sample Part 88','This is an auto-generated sample description for part 88.','2026-05-08 08:00:05',NULL,1),(89,'PN-0089','Sample Part 89','This is an auto-generated sample description for part 89.','2026-05-08 08:00:05',NULL,1),(90,'PN-0090','Sample Part 90','This is an auto-generated sample description for part 90.','2026-05-08 08:00:05',NULL,1),(91,'PN-0091','Sample Part 91','This is an auto-generated sample description for part 91.','2026-05-08 08:00:05',NULL,1),(92,'PN-0092','Sample Part 92','This is an auto-generated sample description for part 92.','2026-05-08 08:00:05',NULL,1),(93,'PN-0093','Sample Part 93','This is an auto-generated sample description for part 93.','2026-05-08 08:00:05',NULL,1),(94,'PN-0094','Sample Part 94','This is an auto-generated sample description for part 94.','2026-05-08 08:00:05',NULL,1),(95,'PN-0095','Sample Part 95','This is an auto-generated sample description for part 95.','2026-05-08 08:00:05',NULL,1),(96,'PN-0096','Sample Part 96','This is an auto-generated sample description for part 96.','2026-05-08 08:00:05',NULL,1),(97,'PN-0097','Sample Part 97','This is an auto-generated sample description for part 97.','2026-05-08 08:00:05',NULL,1),(98,'PN-0098','Sample Part 98','This is an auto-generated sample description for part 98.','2026-05-08 08:00:05',NULL,1),(99,'PN-0099','Sample Part 99','This is an auto-generated sample description for part 99.','2026-05-08 08:00:05',NULL,1),(100,'PN-0100','Sample Part 100','This is an auto-generated sample description for part 100.','2026-05-08 08:00:05',NULL,1);
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
INSERT INTO `printers` VALUES (1,'PRINTER_NAME_STOCK_IN','192.168.245.248',9100,'Label printer for Stamping Press line 1.',0,'2026-05-08 08:00:05.083752','2026-06-18 10:58:01.755705'),(2,'Printer-Line-02','192.168.1.102',9100,'Label printer for Fin Mill line 2.',0,'2026-05-08 08:00:05.083753','2026-05-08 09:36:06.622975'),(3,'Printer-Line-03','192.168.1.103',9100,'Label printer for Tube Mill line 3.',0,'2026-05-08 08:00:05.083753','2026-05-08 09:36:06.622976'),(4,'Printer-Line-04','192.168.1.104',9100,'Label printer for Core Assembly station.',0,'2026-05-08 08:00:05.083753','2026-05-08 09:36:06.622976'),(5,'Printer-Line-05','192.168.1.105',9100,'Label printer for Brazing Furnace exit.',0,'2026-05-08 08:00:05.083753','2026-05-08 09:36:06.622977'),(6,'Printer-Line-06','192.168.1.106',9100,'Label printer for Tank Assembly station.',0,'2026-05-08 08:00:05.083753','2026-05-08 09:36:06.622977'),(7,'Printer-Line-07','192.168.1.107',9100,'Label printer for Leakage Testing area.',0,'2026-05-08 08:00:05.083753','2026-05-08 09:36:06.622977'),(8,'Printer-Line-08','192.168.1.108',9100,'Label printer for Final Inspection gate.',0,'2026-05-08 08:00:05.083754','2026-05-08 09:36:06.622977'),(9,'Printer-Line-09','192.168.1.109',9100,'Label printer for Packaging station.',0,'2026-05-08 08:00:05.083754','2026-05-08 09:36:06.622977'),(10,'Printer-Line-10','192.168.245.248',9100,'Label printer for Shipping dock.',0,'2026-05-08 08:00:05.083754','2026-06-18 10:58:01.755707');
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
  `value_number` decimal(18,4) DEFAULT NULL,
  `value_text` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `value_boolean` tinyint(1) DEFAULT NULL,
  `created_at` datetime(6) NOT NULL,
  `updated_at` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `ix_process_log_details_parameter_id` (`parameter_id`),
  KEY `ix_process_log_details_process_id` (`process_id`),
  KEY `idx_process_parameter` (`process_log_id`,`process_id`,`parameter_id`),
  CONSTRAINT `fk_process_log_details_parameters_parameter_id` FOREIGN KEY (`parameter_id`) REFERENCES `parameters` (`id`) ON DELETE RESTRICT,
  CONSTRAINT `fk_process_log_details_process_logs_process_log_id` FOREIGN KEY (`process_log_id`) REFERENCES `process_logs` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_process_log_details_processes_process_id` FOREIGN KEY (`process_id`) REFERENCES `processes` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=332 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `process_log_details`
--

LOCK TABLES `process_log_details` WRITE;
/*!40000 ALTER TABLE `process_log_details` DISABLE KEYS */;
INSERT INTO `process_log_details` VALUES (260,30,30,106,NULL,NULL,1,'2026-06-08 09:50:50.663953',NULL),(261,30,30,106,NULL,NULL,1,'2026-06-08 09:50:50.663954',NULL),(262,30,30,106,NULL,NULL,1,'2026-06-08 09:50:50.663954',NULL),(263,30,30,107,NULL,NULL,1,'2026-06-08 09:50:50.663954',NULL),(264,30,30,107,NULL,NULL,1,'2026-06-08 09:50:50.663954',NULL),(265,30,30,108,NULL,NULL,1,'2026-06-08 09:50:50.663954',NULL),(266,30,32,112,NULL,NULL,0,'2026-06-08 09:50:50.663954',NULL),(267,30,32,112,NULL,NULL,0,'2026-06-08 09:50:50.663954',NULL),(268,30,32,112,NULL,NULL,1,'2026-06-08 09:50:50.663954',NULL),(269,30,32,113,NULL,NULL,0,'2026-06-08 09:50:50.663954',NULL),(270,30,32,113,NULL,NULL,1,'2026-06-08 09:50:50.663954',NULL),(271,30,32,114,47.3965,NULL,NULL,'2026-06-08 09:50:50.663955',NULL),(272,30,32,114,38.5269,NULL,NULL,'2026-06-08 09:50:50.663955',NULL),(273,30,32,114,23.1137,NULL,NULL,'2026-06-08 09:50:50.663955',NULL),(274,31,35,125,NULL,NULL,1,'2026-06-08 09:50:50.663955',NULL),(275,31,35,125,NULL,NULL,1,'2026-06-08 09:50:50.663955',NULL),(276,31,35,126,NULL,NULL,1,'2026-06-08 09:50:50.663955',NULL),(277,31,35,127,55.0506,NULL,NULL,'2026-06-08 09:50:50.663955',NULL),(278,31,35,128,NULL,NULL,1,'2026-06-08 09:50:50.663956',NULL),(279,31,35,128,NULL,NULL,1,'2026-06-08 09:50:50.663956',NULL),(280,31,34,121,NULL,NULL,1,'2026-06-08 09:50:50.663956',NULL),(281,31,34,121,NULL,NULL,0,'2026-06-08 09:50:50.663956',NULL),(282,31,34,122,22.5540,NULL,NULL,'2026-06-08 09:50:50.663956',NULL),(283,31,34,123,38.9999,NULL,NULL,'2026-06-08 09:50:50.663956',NULL),(284,31,34,123,37.9221,NULL,NULL,'2026-06-08 09:50:50.663956',NULL),(285,31,34,123,59.6790,NULL,NULL,'2026-06-08 09:50:50.663956',NULL),(286,31,34,124,65.5364,NULL,NULL,'2026-06-08 09:50:50.663956',NULL),(287,31,34,124,21.7340,NULL,NULL,'2026-06-08 09:50:50.663957',NULL),(288,32,32,112,NULL,NULL,1,'2026-06-08 09:50:50.663957',NULL),(289,32,32,113,NULL,NULL,1,'2026-06-08 09:50:50.663957',NULL),(290,32,32,113,NULL,NULL,1,'2026-06-08 09:50:50.663957',NULL),(291,32,32,114,69.7931,NULL,NULL,'2026-06-08 09:50:50.663957',NULL),(292,32,36,129,NULL,NULL,1,'2026-06-08 09:50:50.663957',NULL),(293,32,36,130,NULL,NULL,1,'2026-06-08 09:50:50.663957',NULL),(294,33,32,112,NULL,NULL,1,'2026-06-08 09:50:50.663958',NULL),(295,33,32,113,NULL,NULL,1,'2026-06-08 09:50:50.663958',NULL),(296,33,32,114,67.0016,NULL,NULL,'2026-06-08 09:50:50.663958',NULL),(297,33,32,114,47.5362,NULL,NULL,'2026-06-08 09:50:50.663958',NULL),(298,33,32,114,29.2100,NULL,NULL,'2026-06-08 09:50:50.663958',NULL),(299,33,35,125,NULL,NULL,1,'2026-06-08 09:50:50.663958',NULL),(300,33,35,126,NULL,NULL,1,'2026-06-08 09:50:50.663958',NULL),(301,33,35,126,NULL,NULL,1,'2026-06-08 09:50:50.663958',NULL),(302,33,35,126,NULL,NULL,1,'2026-06-08 09:50:50.663958',NULL),(303,33,35,127,54.8678,NULL,NULL,'2026-06-08 09:50:50.663958',NULL),(304,33,35,127,40.3342,NULL,NULL,'2026-06-08 09:50:50.663959',NULL),(305,33,35,128,NULL,NULL,1,'2026-06-08 09:50:50.663959',NULL),(306,33,35,128,NULL,NULL,1,'2026-06-08 09:50:50.663959',NULL),(307,34,32,112,NULL,NULL,0,'2026-06-08 09:50:50.663959',NULL),(308,34,32,112,NULL,NULL,1,'2026-06-08 09:50:50.663959',NULL),(309,34,32,113,NULL,NULL,1,'2026-06-08 09:50:50.663959',NULL),(310,34,32,113,NULL,NULL,1,'2026-06-08 09:50:50.663959',NULL),(311,34,32,113,NULL,NULL,1,'2026-06-08 09:50:50.663959',NULL),(312,34,32,114,37.0354,NULL,NULL,'2026-06-08 09:50:50.663960',NULL),(313,34,32,114,33.1315,NULL,NULL,'2026-06-08 09:50:50.663960',NULL),(314,34,32,114,29.0323,NULL,NULL,'2026-06-08 09:50:50.663960',NULL),(315,34,31,109,NULL,NULL,1,'2026-06-08 09:50:50.663960',NULL),(316,34,31,110,61.0172,NULL,NULL,'2026-06-08 09:50:50.663960',NULL),(317,34,31,111,67.0880,NULL,NULL,'2026-06-08 09:50:50.663960',NULL),(318,34,31,111,67.7720,NULL,NULL,'2026-06-08 09:50:50.663960',NULL),(319,34,31,111,30.8306,NULL,NULL,'2026-06-08 09:50:50.663960',NULL),(320,34,30,106,NULL,NULL,0,'2026-06-08 09:50:50.663960',NULL),(321,34,30,106,NULL,NULL,1,'2026-06-08 09:50:50.663960',NULL),(322,34,30,107,NULL,NULL,1,'2026-06-08 09:50:50.663961',NULL),(323,34,30,107,NULL,NULL,1,'2026-06-08 09:50:50.663961',NULL),(324,34,30,108,NULL,NULL,1,'2026-06-08 09:50:50.663961',NULL),(325,34,30,108,NULL,NULL,0,'2026-06-08 09:50:50.663961',NULL),(326,35,32,112,999.9900,'REJECTED: Pressure drop detected',0,'2026-06-08 09:50:50.663961',NULL),(327,35,32,113,999.9900,'REJECTED: Pressure drop detected',0,'2026-06-08 09:50:50.663961',NULL),(328,35,32,114,999.9900,'REJECTED: Pressure drop detected',0,'2026-06-08 09:50:50.663961',NULL),(329,36,32,112,12.5000,'RE-CHECKED: OK',1,'2026-06-08 09:50:50.663962',NULL),(330,36,32,113,12.5000,'RE-CHECKED: OK',1,'2026-06-08 09:50:50.663962',NULL),(331,36,32,114,12.5000,'RE-CHECKED: OK',1,'2026-06-08 09:50:50.663962',NULL);
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
  `issue_no` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `created_at` datetime(6) NOT NULL,
  `updated_at` datetime(6) DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL DEFAULT '0',
  `process_id` int NOT NULL DEFAULT '0',
  PRIMARY KEY (`id`),
  KEY `ix_process_logs_process_id` (`process_id`)
) ENGINE=InnoDB AUTO_INCREMENT=37 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `process_logs`
--

LOCK TABLES `process_logs` WRITE;
/*!40000 ALTER TABLE `process_logs` DISABLE KEYS */;
INSERT INTO `process_logs` VALUES (30,'ISS-00030','2026-06-08 09:50:50.663953',NULL,1,0),(31,'ISS-00029','2026-06-08 09:50:50.663955',NULL,1,0),(32,'ISS-00028','2026-06-08 09:50:50.663957',NULL,1,0),(33,'ISS-00027','2026-06-08 09:50:50.663957',NULL,1,0),(34,'ISS-00026','2026-06-08 09:50:50.663959',NULL,1,0),(35,'ISS-00027-R','2026-06-08 09:50:50.663961',NULL,0,0),(36,'ISS-00027','2026-06-08 09:50:50.663961',NULL,1,0);
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
INSERT INTO `process_parameters` VALUES (30,106),(30,107),(30,108),(31,109),(31,110),(31,111),(32,112),(32,113),(32,114),(33,115),(33,116),(33,117),(33,118),(33,119),(33,120),(34,121),(34,122),(34,123),(34,124),(35,125),(35,126),(35,127),(35,128),(36,129),(36,130);
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
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=37 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `processes`
--

LOCK TABLES `processes` WRITE;
/*!40000 ALTER TABLE `processes` DISABLE KEYS */;
INSERT INTO `processes` VALUES (30,'CLINCHING_SHORT_SIDE','CLINCHING SHORT SIDE','Process for clinching the short side of radiator.',1,'2026-06-08 02:50:46',NULL),(31,'CLINCHING_LONG_SIDE','Clincing long side','Process for clinching the long side of radiator.',1,'2026-06-08 02:50:46',NULL),(32,'HE_LEAK','He Leak','Helium leak testing process.',1,'2026-06-08 02:50:46',NULL),(33,'M_FAN_ASSY','M Fan Assy','Main fan assembly process.',1,'2026-06-08 02:50:46',NULL),(34,'M_FAN_INSPECTION','M Fan Characteristics Inspection','Inspection of main fan operational characteristics.',1,'2026-06-08 02:50:46',NULL),(35,'ECM_ASSY','Ecm Assy','Electronic Control Module assembly process.',1,'2026-06-08 02:50:46',NULL),(36,'FINAL_INSPECTION','Final Inspection','Final quality gate and inspection.',1,'2026-06-08 02:50:46',NULL);
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
) ENGINE=InnoDB AUTO_INCREMENT=106 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `refresh_tokens`
--

LOCK TABLES `refresh_tokens` WRITE;
/*!40000 ALTER TABLE `refresh_tokens` DISABLE KEYS */;
INSERT INTO `refresh_tokens` VALUES (1,'C7UXPh2fK8XmPze30+qkKmG/Ypn7tNFR2YPkmef67zFHYvVhQa1FDw/aOa0A/XapfpYpWS3C4G6xg1/zZ+tt2A==','2026-05-15 07:59:50.868876',0,NULL,1,'2026-05-08 07:59:50.899042',NULL),(2,'2p5OBwwur6XLO91JsijrAc05I6wJnzVRxVhZffDUWXx7RHWKjmylNMZmqTbk2sodRM3n51PCBZfYNEFMSyX0gQ==','2026-05-15 07:59:51.111279',0,NULL,2,'2026-05-08 07:59:51.111420',NULL),(3,'e7cY69nSyUKve0kvPOSY5EITaDcM8TpU8uTcgCr7KC6ryyLZEqQ+ls/R99mVY3pyP0IQGT6VkRbI1VFtggLbfg==','2026-05-15 07:59:51.316203',0,NULL,3,'2026-05-08 07:59:51.316419',NULL),(4,'cbtqM3IWp2wQ5B/4IuwOkxkpDG3RU9MlGr2+nLn3LbViXBB5KpQPHE4uRIhf/RsFuvqeYbeXAs1ysMMvDRFqeA==','2026-05-15 07:59:51.537242',0,NULL,4,'2026-05-08 07:59:51.537462',NULL),(5,'IDxxolCmVfdeBCwyKjt7Hq5Jf2tiwfUjT777CGrecjxYgHcCAkkltBIFGFU2eJ9mtT3PaBiRdUD8lUu2F+/M6w==','2026-05-15 07:59:51.737340',0,NULL,5,'2026-05-08 07:59:51.737562',NULL),(6,'OiA0F62MR8PVk3nvO6FaDtuTKOuuYrtVYkW5EZsfaQlVq/dj+OmdDpPzt4CIxveQznIAgQ2j/YOJABCTQ/ibCg==','2026-05-15 07:59:51.876702',0,NULL,6,'2026-05-08 07:59:51.876901',NULL),(7,'LMBb9gNw4efE+KyzgOi329skYTiW4kf3SLnrgyrTeSIyI0kawhnklROjyn4+F3Dm7CI7NGCbxGPDmc8BpgUDgg==','2026-05-15 07:59:52.009273',0,NULL,7,'2026-05-08 07:59:52.009425',NULL),(8,'IiqCAl77qLOpV+WE+n5epHaCKZJlr0CgNRO/rCTDp+DkcYTALxLQB4OSS/LJAnjxMV4yurcTARHMiClxbWAGsQ==','2026-05-15 07:59:52.134485',0,NULL,8,'2026-05-08 07:59:52.134607',NULL),(9,'yu63oY7nO/8OiLa+c1/MCZaC7hODxOQ58CWWH/CfDLhpgZ4E0CAjQaD1JqEwxUnVPMnsQN0nVG/rDomzMe2s2A==','2026-05-15 07:59:52.260936',0,NULL,9,'2026-05-08 07:59:52.261101',NULL),(10,'TIUTJTXL6rzGJPA9olP/yyE7nDNyCSHjEoP7A252Jm19GFYyLVEbCHFBZKMCpBcbwr8c4UyFRZYMXCdOqxrPWQ==','2026-05-15 07:59:52.390125',0,NULL,10,'2026-05-08 07:59:52.390273',NULL),(11,'gPeHKzT61n75gowHlPzGJaZcwekA6Ix577HAsyDSLxHJp8anrEK5Lbl+q/Geo2jFwVo+mJzRPrkTRIeNxDj9Hw==','2026-05-15 07:59:52.524796',0,NULL,11,'2026-05-08 07:59:52.524947',NULL),(12,'o8DJV/u7Z/4Z5s/MIPnoB4yOxBxSdKzm6gb0TnPplEFTdJXodih3S/uS/alw2Bqx7O4G3E5hvD7D8U+0cbT5kg==','2026-05-15 07:59:52.649601',0,NULL,12,'2026-05-08 07:59:52.649804',NULL),(13,'+V0wqAsT+mzI/A74cX5BqH/XYUS4SsxB/Vo2qbWPWFfRzGhL/ige+vZVYU6rejO0gDm+Mh8w/+yFJqSpbH42EA==','2026-05-15 07:59:52.780525',0,NULL,13,'2026-05-08 07:59:52.780716',NULL),(14,'UmWtQ20NnNBrwa0Sv1Cr5os1J9RtXTi43ShETYT9VGKdcgsCRh1+tn+R2hhPJ9G8KcBjS4TncAiq9mZSJ1K2Gg==','2026-05-15 07:59:52.908642',0,NULL,14,'2026-05-08 07:59:52.908847',NULL),(15,'FwRW9xROXh4ruepqLfIG0i0OnELTrv1MHKOlMUStzpadGtgFLLoMgOBxAWBmuhNR2qZYbLUZPQjeF9zt7WEpwQ==','2026-05-15 07:59:53.041816',0,NULL,15,'2026-05-08 07:59:53.042032',NULL),(16,'udTS+SrWQhqkwwbv3TsF8rdlmzu2/Jaz/qBo1oZj/yfg+XGgNgcdYxtrYCcrOQaPGYqxgfreL94Tuu7KwvmXTA==','2026-05-15 07:59:53.168276',0,NULL,16,'2026-05-08 07:59:53.168469',NULL),(17,'zseC4WXqIJq2WR9SbjVuD9d379zEnOpQxWx9qz2KB9vDVc915Mp6L21MII02K2zZcZNuIDTimxdlvBZM4vlsxw==','2026-05-15 07:59:53.294102',0,NULL,17,'2026-05-08 07:59:53.294615',NULL),(18,'epfQxly+WLdr1fr9geCtyTv2PgM3ZlhdwvG4q269kb/2u5VpjQ9/u5gTxcb5uKh4ZTCp6BsDkOhvnOhvncGDTQ==','2026-05-15 07:59:53.424428',0,NULL,18,'2026-05-08 07:59:53.424653',NULL),(19,'rro18MWe5XpyzLTJDpaAzzqy4lyRXXoa+NbwM52QbB1Mi5A76B236a1XDKQ0uAB8Fuul2nhahlY74x6c9i1gzQ==','2026-05-15 07:59:53.553566',0,NULL,19,'2026-05-08 07:59:53.553806',NULL),(20,'a6CL69Gi9KUtld25UAeSZT7tJokHs/zLb+VG06A3qPWho3gPu16LNEufBUoAj2U0R8Yx+M23YF+H+4l7B66Zlw==','2026-05-15 07:59:53.721299',0,NULL,20,'2026-05-08 07:59:53.721522',NULL),(21,'Uv8DYPQbiWVJ0WZ0eGqRIvRGhxoY2P0FNw26pKAiybFdZDJCeeibXv2N9hZpwVnGa6CbXyr84wFcYOUQFcpEog==','2026-05-15 07:59:53.847310',0,NULL,21,'2026-05-08 07:59:53.847451',NULL),(22,'vUp8C9jajG+/DiGv3vcOsN7s0bWKNfyjl71jv/iZ3LvgFwwtWsH/VLR0jq6PZeu+H3WBXAs1Kh+TUAv9NZgiQw==','2026-05-15 07:59:53.995841',0,NULL,22,'2026-05-08 07:59:53.995979',NULL),(23,'/2A0gMu0szsL4cE25aZNdctC8QTbu3XXag7VfhMhcFWORpmBtbPlxDBSpRVJNmbbu8UIp9pnnUKAmocEUh/EBA==','2026-05-15 07:59:54.123058',0,NULL,23,'2026-05-08 07:59:54.123329',NULL),(24,'O9q08kg3TtEcyshk2dg+CaAfW/OlSsw07ixNCj89Dz0132y7tRwxVwhTKT4GpIaaA+OxBrrPauziNwN8qrKYPg==','2026-05-15 07:59:54.253432',0,NULL,24,'2026-05-08 07:59:54.253608',NULL),(25,'5yuia9/ohf+6dAxkEyb0GXZ4qK7Vcsq0NzlXtWsSWvcJxHA34qCrVEbuxxbLd33mS32Slgn4Se2BjF4LZCxubw==','2026-05-15 07:59:54.406962',0,NULL,25,'2026-05-08 07:59:54.407117',NULL),(26,'WosubN8O/A8+5urKM+0uZundodhJFuBD93A9su/RcX7qFoLn65LnEo36RguV+2EnrwwkK8uy39R+nekGsYl8Ww==','2026-05-15 07:59:54.536907',0,NULL,26,'2026-05-08 07:59:54.537093',NULL),(27,'ZVQU2BPowiCOB2ag7CnLvRtZ4mCK8u7961QJPPgX4xYyzjsAQS47Y7sW7G22QHO0mcW/pbSi5N0CT1j3OcoO/w==','2026-05-15 07:59:54.660667',0,NULL,27,'2026-05-08 07:59:54.660878',NULL),(28,'g+afAIdbyTi29uRUXiVynpV/7WggViVrm1v/rZTITpJJbSMJzrT29Hqu8oWv9zZ0w8ybjnVbiP99SPtWCe9opg==','2026-05-15 07:59:54.791741',0,NULL,28,'2026-05-08 07:59:54.792039',NULL),(29,'Ch3by6h+RIXki92KJxsTCYf4qWPpUIyqf4Z+hg6VqTXYdExAi1WKPsH9fprV9xmJc1n7XYldcHXMjEIjhNTk0Q==','2026-05-15 07:59:54.921118',0,NULL,29,'2026-05-08 07:59:54.921282',NULL),(30,'OT1VK/pS0Ustkhb+jK21T2knR+57m/IS8Oq/1xJEOjW++NeSJH90D4Q6B5v9CJJQH/XoOkUcyRKe1deE88HK6Q==','2026-05-15 07:59:55.053590',0,NULL,30,'2026-05-08 07:59:55.053762',NULL),(31,'3lqICX+shyZnhpK/BHIANTJSkh9MqP7JGTXF+WtmRlBoQyX0vDqdY+ZW1rjFHe/soLk3ZWbj2KXDHoSTR5xmig==','2026-05-15 07:59:55.195958',0,NULL,31,'2026-05-08 07:59:55.196059',NULL),(32,'LCHVMIRfTnXLa4qWFjizvxA+9RBeLCxEW4mo3X0EMh7qOaj+KwFkX+ZEuy3l48OtAMrI47SmQnC28S8fV858YA==','2026-05-15 07:59:55.344483',0,NULL,32,'2026-05-08 07:59:55.344931',NULL),(33,'XobenBWzDFjHT6WXCFJjD6fGgkR8n0cPVFuMX2Ps2Xz/ITEtmpDj+DDJV+kLQyoc95f6GPwZzL8gzSQyJZ/jhA==','2026-05-15 07:59:55.487916',0,NULL,33,'2026-05-08 07:59:55.488113',NULL),(34,'UuHflfkwTZdrTSshrxvQh4JG8rjY0O1ZKop0ndkqv0PjeJQeO8M1ctseEmZF+VCuUxyzqBbIZpP6qrpCpdSDpw==','2026-05-15 07:59:55.628658',0,NULL,34,'2026-05-08 07:59:55.628845',NULL),(35,'Hnnqe5Dpq4TOQH2JPJSppTJC8vqczLELBYMo+ZkzMiJSzYtgrTBRNrut3UBcccDRf8ATCMvevW8qaKo66gaOtA==','2026-05-15 07:59:55.774886',0,NULL,35,'2026-05-08 07:59:55.775081',NULL),(36,'iHLm5tXM9BtpxzkQ1giCtLCG5bvyB7uRrXBeMsHwXa1sc4WCflyUR/x0FSkYMXUWKG8a6QZj8yZkv2JSbRG+xg==','2026-05-15 07:59:55.904179',0,NULL,36,'2026-05-08 07:59:55.904300',NULL),(37,'rxqiyJo9J+/mhDBdfrJKQPrzUrx+9rlCcnVRMdFda0W09FP8s06wMs6isrwSik8sQiGPx5220h6WCCo9hPV6fg==','2026-05-15 07:59:56.041224',0,NULL,37,'2026-05-08 07:59:56.041337',NULL),(38,'AFzo3FvWRgJCOP1ikePqtgsyfzbyeRvODVuwGC/4c5uLdMRSHuBjlf55P1D7Mfa5KBUm7CLaMA6E1pQaMDM+BA==','2026-05-15 07:59:56.163644',0,NULL,38,'2026-05-08 07:59:56.163905',NULL),(39,'tW7UnEVL8dTio9aW+8Y6UTiybWUA6mvvdvIn3DUVTFmDepbEwXegiNS4jiqVxlpPK26akB5KdcPKhfFr8UTrqg==','2026-05-15 07:59:56.320780',0,NULL,39,'2026-05-08 07:59:56.320998',NULL),(40,'11gKUES3s6Ya/P+0Tza0mzRxM5BcGJ1tzCOl8X8IohmQeushcti/lgun0v0jKHRoRt5y1C9Yz0XnYEAjDKl4EA==','2026-05-15 07:59:56.450849',0,NULL,40,'2026-05-08 07:59:56.450953',NULL),(41,'wRUey8H9SusSXR3d2Z9jpB9DZ/4SaalnGsEd9nZAgSeOzXhcgcxd6X7Ckn0aqYk+PypuRDPPMn3WOLTMypeG4Q==','2026-05-15 07:59:56.575951',0,NULL,41,'2026-05-08 07:59:56.576093',NULL),(42,'o0Z8hCkXz1PcVMc86gEVb9gq+HVxuDo0yN9gQbSGCPpD+I+IHTuv/jMP8ZQwc+KB1fdalN/FhSLgLcwYNk7m+g==','2026-05-15 07:59:56.710542',0,NULL,42,'2026-05-08 07:59:56.710696',NULL),(43,'YTrxixPWLC77P+dyTYoKvrYO97WPe1LpZnO5UYERqzu/I0DLfWVF7hXxVLywb/Ng18xD2m53Y/aNw7WpWrDDTA==','2026-05-15 07:59:56.837321',0,NULL,43,'2026-05-08 07:59:56.837484',NULL),(44,'3amMS0SZexR42V2wXMPU7t5aZH4QNUp4s99X0x4DiTfnUb7yJnr/3mEz1Mv7l8D1J0WNjJO85Que0J9CCpcHDw==','2026-05-15 07:59:56.971599',0,NULL,44,'2026-05-08 07:59:56.971771',NULL),(45,'Rh5ri8iNUFAFKpJ8PBqJfd/R7WSPqTMNAcFlrJbU8FsWNTiFsfr+jP4uXVV8hClQyLfDc4/dfSoFj11BwEb6Yw==','2026-05-15 07:59:57.096524',0,NULL,45,'2026-05-08 07:59:57.096647',NULL),(46,'CNkEwcHmBYyR5gdV4JDm5f5ZisnzuAtavdbLGJPQyVVp8yxaUuFcJlx9LzQMz8tNIRRLqLfAhUHmxv2ZF3Ihig==','2026-05-15 07:59:57.223784',0,NULL,46,'2026-05-08 07:59:57.224097',NULL),(47,'j76B68x70HM32ruAdyqwl8CZQqSNj+nIn+YYy4Izpll3knTcDS0fw40LePcvNf1+LX6zIooIeuzPzsPMx5iShw==','2026-05-15 07:59:57.347451',0,NULL,47,'2026-05-08 07:59:57.347656',NULL),(48,'eD5QuFGvv6JrBq9FKOuxLKz2MDP7eBsBLdcnIGXz/hebm1vjuqGnGpfBfsqXc1itpfkUXFVTKVVeQkcE6GDpMQ==','2026-05-15 07:59:57.472556',0,NULL,48,'2026-05-08 07:59:57.472700',NULL),(49,'F9Wp1geNW+SuiYDAOBPDmPnKZxk4+qNdPPBEH2sm4oQ5AlrGik9Mo2sJGqfNymVKaLyACa7QQj19Px4qItjygA==','2026-05-15 07:59:57.598653',0,NULL,49,'2026-05-08 07:59:57.598781',NULL),(50,'OJCBGtVpd485h3Fw+rQNhCtXGXIGSDp6axiav64f6O1vSSPKtqfkl6m4Vd6s1epRW1bZZ4+p9DobsL9cGPXlqA==','2026-05-15 07:59:57.747785',0,NULL,50,'2026-05-08 07:59:57.748017',NULL),(51,'qZbe7wDAcA1xxDejS2Y9nmBzDmIY4M516OcEZ8ZG9gaE+xS8vr+fvUkbSnk2xfo4TUWDNmPKRDYmnSGV1SIhhg==','2026-05-15 07:59:57.875886',0,NULL,51,'2026-05-08 07:59:57.876104',NULL),(52,'n5chV/ITaeMQTXP/fDJ9R7r33qhvIUDjskizopI4pq3jA0O+5TSfm5f7XrB+rY4kASVmDT/jBlFyaU2PqW4fLA==','2026-05-15 07:59:58.001810',0,NULL,52,'2026-05-08 07:59:58.002172',NULL),(53,'6FDMpm7s7QCUdsyw7Mu6J055Er5lF1VdZFZUz3DgNMz9yc2vPOuuRySHif35Kk/oE9SecjgzGeBYFqEhgILowA==','2026-05-15 07:59:58.160773',0,NULL,53,'2026-05-08 07:59:58.160921',NULL),(54,'EksHbwyxAv7KmlpiRK+jPzg1OefpD3HiJtkIuti6b50iUFzXGxIc/ruhDVtQbpL6OmBEdTSutxIJ8eZUqm4cNA==','2026-05-15 07:59:58.285414',0,NULL,54,'2026-05-08 07:59:58.285640',NULL),(55,'UYSPeorfo4FbYsAn6NeYl3qFpY/mxVZFl5zVlRLDISeMkw/VC0QR8IItQ6ZkgiSf+FHAaHZlXZiOSMg8Am4Ivg==','2026-05-15 07:59:58.412046',0,NULL,55,'2026-05-08 07:59:58.412200',NULL),(56,'Cgk2NV0nZrWEV4LgndiDnVyjbHDeXWmEv6xaV+TCzXNumq/tvqeAcYZdyOzvjuX+af9Hgy4dLGLafDyqzmahCA==','2026-05-15 07:59:58.542712',0,NULL,56,'2026-05-08 07:59:58.542837',NULL),(57,'FFw0nWLf1AjbE9QqPcNyPHlKDRivZYpEoVZISkHrc0bl0HkqULnqs3+9wemhIfiLJGAEFgYwjM43MUfPtx8v/Q==','2026-05-15 07:59:58.671238',0,NULL,57,'2026-05-08 07:59:58.671391',NULL),(58,'BGVZ+vJi57X/tq3dGfRSutVY4IuEP3qhn6iGykMonccHi+HQSbMwYJIMOOTwsp42pNM/F4nz7/Uf6mFnHqZ0eg==','2026-05-15 07:59:58.798967',0,NULL,58,'2026-05-08 07:59:58.799115',NULL),(59,'J5RQa8N/bDX4jZ6s6r+2Fn5ACWPA2OlKS0JyP1sN6kLjfGzw3Nvfg9c4j/pgIeDvE0A5IdRNKVFAYQ3emJkXqQ==','2026-05-15 07:59:58.932994',0,NULL,59,'2026-05-08 07:59:58.933137',NULL),(60,'4SPItET52nXUSODvpP3Y0Nl9EE4eNDai/nWGopEci/MOb/z7TZBIq3f6z09IPIPeJejiemfOP2+SeN7f8JtydA==','2026-05-15 07:59:59.088080',0,NULL,60,'2026-05-08 07:59:59.088253',NULL),(61,'jjVEYUHEh+AoHiHxzSillZJcUbqvEbx8/z+HEUL5to0ySfhVi4LqhaJ9EpIFUKdklT/GJeOsR05g4IZ2O6QqgA==','2026-05-15 07:59:59.230327',0,NULL,61,'2026-05-08 07:59:59.230627',NULL),(62,'K6rUKfSdRSyNMT00cUHyIQ4YF+ulVg9rlzYV19RCpT4LmMfvHJDtt3muCm3BPkpnDVbdgMWQ7RYY1xD+cVhIhw==','2026-05-15 07:59:59.361520',0,NULL,62,'2026-05-08 07:59:59.361667',NULL),(63,'n/JfFut+lTNDo7pDCpFHdMcQ8tGDDZnQ3kZhgHrqDvh8uLVBAcFzdkniO/ObnTxvIC0PvXpwVd+Q2aIucClx9Q==','2026-05-15 07:59:59.496071',0,NULL,63,'2026-05-08 07:59:59.496201',NULL),(64,'RtOqHIPoLGXwROsh3PlyCGBoApAWQ2mZm5fluRjkUBoIk+KYt8Vzn9IRttk6b0fedPgCGWpJMtLeigWyfCq4YA==','2026-05-15 07:59:59.653642',0,NULL,64,'2026-05-08 07:59:59.654015',NULL),(65,'85XZB7APT03ao5iMWUPfzYBOTS+X9LsKKOSvOsV0AmCCBSfhbm4CsQPqfroAhAq6QZxjuS/G4TOxIdQTnuVM8Q==','2026-05-15 07:59:59.801670',0,NULL,65,'2026-05-08 07:59:59.801918',NULL),(66,'lMTcJmuUnt5vXnsYCtzlflcaYKc237aQsKTW48G+YobN0pLYRlOj68EFbLVBqZyOB5WztYpibzxSwgNOuUF01w==','2026-05-15 07:59:59.940839',0,NULL,66,'2026-05-08 07:59:59.940991',NULL),(67,'i3PscciKqfXxepY+gVFmGPpegY3BqCfGaLc0RQy4xQ16/uDw1mokBL7SSXV7/lx0cWWlKIKPwfJ0LfFFXu+xbQ==','2026-05-15 08:00:00.072072',0,NULL,67,'2026-05-08 08:00:00.072267',NULL),(68,'n9BwNzZX91kqZdgsHj0xechNMjMvThPJUQxlH8qzReET02MwPriX0sVF8kOLBoa4wHO0cM1dO1uxEXnMqFs11A==','2026-05-15 08:00:00.196367',0,NULL,68,'2026-05-08 08:00:00.196547',NULL),(69,'rNkmOavVU5ll/87HeGin9Y54G/RL31VlJWRsCix72saw2mf9YLO0ZYV8ktrpG6HF9UHVnXyonHZMD4iP10gwyA==','2026-05-15 08:00:00.320364',0,NULL,69,'2026-05-08 08:00:00.320556',NULL),(70,'DelTaOXKiEmsFeLt1zhapkZRLTzDXZob+A3wHF/PTfCMQ91o1uGuYaZZxejeA3LpBaujXRvCwlUXM+vdZMR2Uw==','2026-05-15 08:00:00.454177',0,NULL,70,'2026-05-08 08:00:00.454363',NULL),(71,'VgHHWyEv+fwXhAcUswYh166QD5/+mCxvOX9WqAK0F9TgPtMZLGm3FatS+GjXHFigIt1eUG+8UhBOhMFpj9HG+A==','2026-05-15 08:00:00.592946',0,NULL,71,'2026-05-08 08:00:00.593330',NULL),(72,'pB2R8jQrYtv8PMqj8VJKYCu+DoCaAVUB2cliIX8R97Esi5ShV4fdIu5/CKpRNUwsglSlsGers/W1zMQ5kiqd6Q==','2026-05-15 08:00:00.722913',0,NULL,72,'2026-05-08 08:00:00.723115',NULL),(73,'COQETie4Fz2TFUe0vdvF9iwSarBZNy1KJDXypYCfmUZ14/g/ry65lZYw+PttEJ+ih6xwtXg9jlSkn1I+6vLAhg==','2026-05-15 08:00:00.849233',0,NULL,73,'2026-05-08 08:00:00.849392',NULL),(74,'VjhCqhC9MwEAPop/9nk3SHDMPpp3tTCZrWM9274Jlsn5IgaJf+Way97rDYT6XWG+Ge8rkBqnTA9TqOI3IbMM0A==','2026-05-15 08:00:00.982699',0,NULL,74,'2026-05-08 08:00:00.982881',NULL),(75,'hm+6wHGQXzAvSUYQxBgqrd+j9pyvrtQhXrR2hxT7QDoEaY7pzER0qGCVrtP57oWZHgN2ik9iocJp2qJJr/XZtA==','2026-05-15 08:00:01.109841',0,NULL,75,'2026-05-08 08:00:01.110021',NULL),(76,'6TWhhZgntgjVJ3C8oKlbuq5J8as+ljL2RtR2wAPM4Kt/X2FYTG/S8FvPwag0wtjLtBnk0IDO+Yg8zjxVFGRSpQ==','2026-05-15 08:00:01.233129',0,NULL,76,'2026-05-08 08:00:01.233302',NULL),(77,'T/sGG6mMQEOMsP3KVyjj4fAPNavcd/TB58Ae2rNu8Vl8UEq64EVZjinNuv9prqIaSDf+ka9JYNfpqY/FM8Dtkg==','2026-05-15 08:00:01.361958',0,NULL,77,'2026-05-08 08:00:01.362136',NULL),(78,'2bi80mFNP153XR+m7+xL4cKOfBB0+ptidc2zktDHDqsyAXrA+YiZkKQSPCOewXEjQCR4WXmFb/i2nJMiKCX47A==','2026-05-15 08:00:01.494174',0,NULL,78,'2026-05-08 08:00:01.494444',NULL),(79,'dUOABG6YHSYriMzk/JLNA6kU8JSexX97obDXDFBBspCAFSjE6tQjqfpj2GsK8vR4XzZDx9mDzIJhrzT6tNOyPw==','2026-05-15 08:00:01.632381',0,NULL,79,'2026-05-08 08:00:01.632532',NULL),(80,'1Gq7tWtJDJJJEg8rRhK3m+XHW/p4U65GDHAke4pMWWyvOSvLMZbDuluHE/tw90lvKHwSGT3l4/liIGgOYptMeQ==','2026-05-15 08:00:01.767557',0,NULL,80,'2026-05-08 08:00:01.767723',NULL),(81,'ot2KJ/n+ZuM5gYSPMoIwyF+4FDaVDgFwaHIMfrseEYwjNVUT8BLk3cPUmwZj3+pO2U5IqBoB/WGt3Fth3glb+A==','2026-05-15 08:00:01.899079',0,NULL,81,'2026-05-08 08:00:01.899259',NULL),(82,'eqdNLKuygP3B+2XpG7UCrYF6jKO3NOyCVe3M2IdvFj9yqUyN8TxnsjWFSewRBb94LGCJWKhhFFDCo3nTUC8QEw==','2026-05-15 08:00:02.044906',0,NULL,82,'2026-05-08 08:00:02.045303',NULL),(83,'wGHPoD0g8ZBjvvRTNPcY3fwle/CZ5XeN2cgh0ZCebMQqQgKx6evreGlpP3bvzc7hqNmJtW+7+oszexxP6D5VBQ==','2026-05-15 08:00:02.177508',0,NULL,83,'2026-05-08 08:00:02.177674',NULL),(84,'O7/IajNhSGNbwjkL3OUYqcd9/lNqEp0gU9+rA0rpU1vICpYZiQ0gnJNCMvEIURulGrxEo/rlpdN66icfhHKnLA==','2026-05-15 08:00:02.306713',0,NULL,84,'2026-05-08 08:00:02.306907',NULL),(85,'ja7RjSNlwSaq00bsfDaPu85n7efDw13uO1sTbAJpq+jXuYYHgp0ue83lATU9ZFje8EKQ9BIumO7I4FDghIzFcQ==','2026-05-15 08:00:02.435128',0,NULL,85,'2026-05-08 08:00:02.435450',NULL),(86,'tZl0xqcEvIsyoeY3JoVid74kHbSx1D8280FfKvyw0WVLSd4HNUA44vnd8zATWH6iv8yXFPJhwHMJ1k0iEGU8bQ==','2026-05-15 08:00:02.562818',0,NULL,86,'2026-05-08 08:00:02.562989',NULL),(87,'jJX9jGNNKMdnNwO4imvtFZixo+h3OBsLBg0LGkF7mbQsNytrQVIhxyWFIbsJPWcKW10D9VSL+eniL7a4b9RX7A==','2026-05-15 08:00:02.687702',0,NULL,87,'2026-05-08 08:00:02.687904',NULL),(88,'7T7JmJ6holC7/CYwlKb9DHGhq351ZT8/+bFvWdRFa2nI5Vy8RrB86OgImHu/9wD7cz2xbgB29yJTY2jrAXv64Q==','2026-05-15 08:00:02.814212',0,NULL,88,'2026-05-08 08:00:02.814401',NULL),(89,'KKX2fYuKdAH/CCh5Ec7BX4twPlg7oG2QuLInFQ9vbLGWsnRgh4LAXiTWEfsaD/H7wJ2smqHFA4HsnFjMweej5Q==','2026-05-15 08:00:02.944604',0,NULL,89,'2026-05-08 08:00:02.944786',NULL),(90,'cc2bL7tPxzaCh+C9a9Ud7UUnBBdH8FSXocHVa35lqnx+BA6S7XP4/Pacu+wbi9CREwfIohXH/scJOHYm+ylnCA==','2026-05-15 08:00:03.072732',0,NULL,90,'2026-05-08 08:00:03.072932',NULL),(91,'scbvIQ24UUJfSQAPuHAMjTrfQP9rEn0BwlVzmo1WPCKTxFQPMzgo7kx4UfCkl3ce4YQ7pp3hbp4G8gSkxD4bjA==','2026-05-15 08:00:03.204811',0,NULL,91,'2026-05-08 08:00:03.205196',NULL),(92,'0tR21ngjn61cesfIGsH4YCUJOpgIsd1IGqVS+y4OdAS2LUyrUHsZGiAhWOEFMMKzu3PuE8uzU+jvKt1EmO0R4w==','2026-05-15 08:00:03.340366',0,NULL,92,'2026-05-08 08:00:03.340576',NULL),(93,'hG1K5YI6qFnoZuLT+brrLdeTMrxW3XiiPRiIOLC85sFEAYtTGiitiSTdp5gOfw4d9FsRlf5ypzL6FMvbGcR89Q==','2026-05-15 08:00:03.475129',0,NULL,93,'2026-05-08 08:00:03.475599',NULL),(94,'VpkwZaq5JEI5XDD2wnxIfYUmrGDpZXGCu7mNojNG/jpCOyKmkKRnlw+Lj53zOP43lY/lQzSB8ZxGMGUhK6WcWg==','2026-05-15 08:00:03.619903',0,NULL,94,'2026-05-08 08:00:03.620292',NULL),(95,'33vHk2KN93HQoHqFqAN42g7e43p5F3k2mqzX+YaBhSawn1tT0I+pgWY62zdlYR3/sTmELlaC3fz+F4jAEMW+mA==','2026-05-15 08:00:03.762777',0,NULL,95,'2026-05-08 08:00:03.763108',NULL),(96,'kPNYZ5sUzGo01hq6W803W/Ap0nlXeTHl18CijeV6csRYFK1BVnlb69w6AtGXjOSFbdOV7y2es1i+ug39i6tttA==','2026-05-15 08:00:03.906287',0,NULL,96,'2026-05-08 08:00:03.906514',NULL),(97,'c+ZwI02ASSFPS2o7jO/lLV5J9KamYrBif9KJJ4IA6qAy31CvTm4LUX7iVIwDAuiUJXVwZ3E5SZpydIni8zbDdw==','2026-05-15 08:00:04.057257',0,NULL,97,'2026-05-08 08:00:04.057527',NULL),(98,'4AQJCkit0o1ga5cd9CcL8IOLb5pyF2G+nAvBK3DlyfazHua7u058QJH66rT2Ohr0ECELUgBILFyyzv1nr9kTvg==','2026-05-15 08:00:04.204555',0,NULL,98,'2026-05-08 08:00:04.204790',NULL),(102,'SeHOMchuIVnKg/khmX2/DA1E+/yy1IX7uEwpKCMRKsyqL8Z+E1QFNs/dAJKIXLSnJ6CYrYHHOf0dojee87WsKA==','2026-06-17 03:31:05.411780',0,NULL,1,'2026-06-10 03:31:05.501081',NULL),(103,'ljs3VAj48SGAm/EZznzjIZimfiZr9i/01H81jHP+IjlGvMyDI3mSrAAV9MkB/2rEZhgIda3jXJQWHnQXX7m71Q==','2026-06-17 03:35:28.427643',0,NULL,1,'2026-06-10 03:35:28.428910',NULL),(104,'mAIuu6bMELqRDgAUbTA2g2kcPGuvYTbhbtqb1+YiFitl73jgs8wHGFmrGJZ2oj2JYMropaLTVaZ7h0IvPNkC1A==','2026-06-17 03:36:53.442473',0,NULL,102,'2026-06-10 03:36:53.442672',NULL),(105,'5E00v9Wux/syP2xnZD/aQ41y2tsYChIJpGup+swpMuMQLYd0ssVYhl/eUa52/mPOoBp+hPFxxSNNmJtUV7KIJg==','2026-06-19 02:36:37.464988',0,NULL,1,'2026-06-12 02:36:37.511867',NULL);
/*!40000 ALTER TABLE `refresh_tokens` ENABLE KEYS */;
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
) ENGINE=InnoDB AUTO_INCREMENT=35 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `stock_ins`
--

LOCK TABLES `stock_ins` WRITE;
/*!40000 ALTER TABLE `stock_ins` DISABLE KEYS */;
INSERT INTO `stock_ins` VALUES (1,'STI-00001',1,500,'2026-04-30 08:00:05.206104',500,'2026-04-30 12:00:05.206182','2026-05-08 08:00:05.213048',NULL),(2,'STI-00002',2,100,'2026-03-26 08:00:05.209333',99,'2026-03-26 12:00:05.209333','2026-05-08 08:00:05.213047',NULL),(3,'STI-00003',3,100,'2026-04-24 08:00:05.209376',97,'2026-04-24 10:00:05.209376','2026-05-08 08:00:05.213047',NULL),(4,'STI-00004',4,300,'2026-04-15 08:00:05.209381',299,'2026-04-15 10:00:05.209381','2026-05-08 08:00:05.213047',NULL),(5,'STI-00005',5,300,'2026-03-20 08:00:05.209386',300,'2026-03-20 13:00:05.209386','2026-05-08 08:00:05.213047',NULL),(6,'STI-00006',6,200,'2026-05-02 08:00:05.209390',200,'2026-05-02 13:00:05.209390','2026-05-08 08:00:05.213047',NULL),(7,'STI-00007',7,1000,'2026-05-05 08:00:05.209393',998,'2026-05-05 13:00:05.209393','2026-05-08 08:00:05.213047',NULL),(8,'STI-00008',8,100,'2026-03-28 08:00:05.209396',96,'2026-03-28 12:00:05.209396','2026-05-08 08:00:05.213047',NULL),(9,'STI-00009',9,100,'2026-04-19 08:00:05.209398',100,'2026-04-19 13:00:05.209398','2026-05-08 08:00:05.213047',NULL),(10,'STI-00010',10,1000,'2026-04-06 08:00:05.209402',1000,'2026-04-06 14:00:05.209402','2026-05-08 08:00:05.213047',NULL),(11,'STI-00011',11,750,'2026-04-29 08:00:05.209405',749,'2026-04-29 09:00:05.209405','2026-05-08 08:00:05.213047',NULL),(12,'STI-00012',12,50,'2026-04-01 08:00:05.209407',47,'2026-04-01 09:00:05.209407','2026-05-08 08:00:05.213047',NULL),(13,'STI-00013',13,150,'2026-04-20 08:00:05.209410',147,'2026-04-20 13:00:05.209410','2026-05-08 08:00:05.213047',NULL),(14,'STI-00014',14,100,'2026-04-02 08:00:05.209413',100,'2026-04-02 12:00:05.209413','2026-05-08 08:00:05.213047',NULL),(15,'STI-00015',15,50,'2026-03-27 08:00:05.209415',48,'2026-03-27 13:00:05.209415','2026-05-08 08:00:05.213046',NULL),(16,'STI-00016',16,150,'2026-04-16 08:00:05.209421',150,'2026-04-16 10:00:05.209421','2026-05-08 08:00:05.213046',NULL),(17,'STI-00017',17,50,'2026-05-03 08:00:05.209423',47,'2026-05-03 12:00:05.209423','2026-05-08 08:00:05.213046',NULL),(18,'STI-00018',18,50,'2026-04-12 08:00:05.209426',47,'2026-04-12 12:00:05.209426','2026-05-08 08:00:05.213046',NULL),(19,'STI-00019',19,250,'2026-03-20 08:00:05.209429',250,'2026-03-20 09:00:05.209429','2026-05-08 08:00:05.213046',NULL),(20,'STI-00020',20,50,'2026-03-30 08:00:05.209431',50,'2026-03-30 09:00:05.209431','2026-05-08 08:00:05.213046',NULL),(21,'STI-00021',21,1200,'2026-04-10 08:00:05.209433',1196,'2026-04-10 11:00:05.209433','2026-05-08 08:00:05.213046',NULL),(22,'STI-00022',22,500,'2026-04-16 08:00:05.209436',500,'2026-04-16 09:00:05.209436','2026-05-08 08:00:05.213046',NULL),(23,'STI-00023',23,250,'2026-04-06 08:00:05.209439',247,'2026-04-06 11:00:05.209439','2026-05-08 08:00:05.213046',NULL),(24,'STI-00024',24,150,'2026-04-03 08:00:05.209441',146,'2026-04-03 14:00:05.209441','2026-05-08 08:00:05.213046',NULL),(25,'STI-00025',25,50,'2026-03-12 08:00:05.209443',46,'2026-03-12 11:00:05.209443','2026-05-08 08:00:05.213046',NULL),(26,'STI-00026',26,750,'2026-05-01 08:00:05.209461',750,'2026-05-01 12:00:05.209461','2026-05-08 08:00:05.213046',NULL),(27,'STI-00027',27,200,'2026-04-29 08:00:05.209463',197,'2026-04-29 09:00:05.209463','2026-05-08 08:00:05.213046',NULL),(28,'STI-00028',28,150,'2026-04-07 08:00:05.209466',148,'2026-04-07 15:00:05.209466','2026-05-08 08:00:05.213046',NULL),(29,'STI-00029',29,750,'2026-03-26 08:00:05.209468',747,'2026-03-26 11:00:05.209468','2026-05-08 08:00:05.213046',NULL),(30,'STI-00030',30,300,'2026-03-30 08:00:05.209471',296,'2026-03-30 09:00:05.209471','2026-05-08 08:00:05.213045',NULL),(31,'ST20260602001',1,100,'2026-06-02 06:17:46.515000',100,'2026-06-02 06:17:46.515000','2026-06-02 06:18:02.036233',NULL),(32,'ST20260603001',1,100,'2026-06-03 04:37:02.661000',100,'2026-06-03 04:37:02.661000','2026-06-03 04:37:14.445486',NULL),(34,'ST20260605001',1,1000,'2026-06-05 03:15:27.942000',1000,'2026-06-05 03:15:27.942000','2026-06-05 03:15:38.702286','2026-06-10 04:03:07.087520');
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
) ENGINE=InnoDB AUTO_INCREMENT=103 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `users`
--

LOCK TABLES `users` WRITE;
/*!40000 ALTER TABLE `users` DISABLE KEYS */;
INSERT INTO `users` VALUES (1,'admin','admin','$2a$11$N8tH/kvyV/9ypodwn8TdRuY.5ep0R.8GCzlhMx.2UmqfQrUhKLyfy','admin',1,'2026-05-08 07:59:50.739649','2026-06-10 03:31:52.678239'),(2,'Dummy User 1','user1','$2a$11$Xy5p9v1.i1grRA5x0WGd9ebocHdEydYkEj9NXgZ/DbDBVfPp5UpFC','user',1,'2026-05-08 07:59:51.106067',NULL),(3,'Dummy User 2','user2','$2a$11$JqJoypr6jvrIjZacsGEufeT2ii0giY.9xW95DcMW6YVFoHMz2P7k6','user',1,'2026-05-08 07:59:51.312676',NULL),(4,'Dummy User 3','user3','$2a$11$iTVNsIROwa7aiPz2AUcdbuOFincJr/yPykFvfAo03Y5bZ4BSvIfX2','user',1,'2026-05-08 07:59:51.532341',NULL),(5,'Dummy User 4','user4','$2a$11$PwJYu1ggnBtGZr6AGsuN5OGeVJUEVhTs1h.Mch.RjB41urXdGLz8e','user',1,'2026-05-08 07:59:51.733285',NULL),(6,'Dummy User 5','user5','$2a$11$earr.Wy/n.6MJDY771.hKObttdS/F/67jNbucwSGNzbj3ZKc9DzuG','user',1,'2026-05-08 07:59:51.871146',NULL),(7,'Dummy User 6','user6','$2a$11$wMsgPkTfRyhJIkb75wSRlui8zKMWWvlMqaYS5hOIxXk/yWntiAPGu','user',1,'2026-05-08 07:59:52.004007',NULL),(8,'Dummy User 7','user7','$2a$11$avsnmljP66nrGEh4S8g2S./5CtBeWpLVmDgCSTkhOfyGOk4Zi.BOy','user',1,'2026-05-08 07:59:52.130496',NULL),(9,'Dummy User 8','user8','$2a$11$7iqY.WGaVTfMcLatDZfKFuYsYdrw6qs2cCS5S3BGR0KN7HMB0n8eS','user',1,'2026-05-08 07:59:52.255508',NULL),(10,'Dummy User 9','user9','$2a$11$ZLw3An98V2o3HpSPzaFD6.pfA3XcGm5LQnMSZOh849Dsi.HJgwUa2','user',1,'2026-05-08 07:59:52.384253',NULL),(11,'Dummy User 10','user10','$2a$11$fOzXTbAqjUfnqSwZEGsQM.G1G43fvfVdIh5h385hf11XzWLOlmt3y','user',1,'2026-05-08 07:59:52.519923',NULL),(12,'Dummy User 11','user11','$2a$11$fvKi.KKCCjgnMkM8YnPXoeZG76OXFOi.d7gI4mf5oSTdpufOCKWlK','user',1,'2026-05-08 07:59:52.646331',NULL),(13,'Dummy User 12','user12','$2a$11$t7vmV3hmktC7IpjMeDpW7eVAXEfRocgAAe42uwCuOatghB1YF5mP2','user',1,'2026-05-08 07:59:52.776077',NULL),(14,'Dummy User 13','user13','$2a$11$soYyDkN5Q0GavyZZzljeUuc7E5/9k4XL7XvG621b3YZ0ydLSgevRa','user',1,'2026-05-08 07:59:52.903695',NULL),(15,'Dummy User 14','user14','$2a$11$3iV6T9GLH0ryI008hguOT.46dbIQR.zSnChcGfcdXn6wCwr7zNUha','user',1,'2026-05-08 07:59:53.036951',NULL),(16,'Dummy User 15','user15','$2a$11$5dT4C./ioOdRLafJ5cdKb.4/pNXzbjBoX5ddQdehkgUdt4rrZeMVW','user',1,'2026-05-08 07:59:53.164117',NULL),(17,'Dummy User 16','user16','$2a$11$BwMF9larGzPN28Rg/2dACOUUnKtIpEUru9RXatQbkuiXtUAKMa3S6','user',1,'2026-05-08 07:59:53.290400',NULL),(18,'Dummy User 17','user17','$2a$11$emFD6hnfhLpeLBlnvaZF6efR4ajjY67Nd43KKcRFkWzt6WoY5XWC.','user',1,'2026-05-08 07:59:53.420140',NULL),(19,'Dummy User 18','user18','$2a$11$krUytaxXm0k5AUueP0NgUO/7lYfTYSCTQ9p.FqeYA7eFzMPeMFuqC','user',1,'2026-05-08 07:59:53.548965',NULL),(20,'Dummy User 19','user19','$2a$11$rqsRonucWLp6f6T07XzDtetdevX53j6ndT2M6eYg.G8C83GrOPbfS','user',1,'2026-05-08 07:59:53.716177',NULL),(21,'Dummy User 20','user20','$2a$11$CwfL4sW/OjBArJOQ4WHsNe6p0VCIazZgVvpBT5QRu9GJzWkAHBD..','user',1,'2026-05-08 07:59:53.843160',NULL),(22,'Dummy User 21','user21','$2a$11$u7sMBiyuWieny5wy53JEmu67ITucrx8xtMhtZfOCcr9jVwVj9tr6e','user',1,'2026-05-08 07:59:53.992284',NULL),(23,'Dummy User 22','user22','$2a$11$Iu8tDNBbPV9un3mj3Kwn6eCYTPN4xL9G5CHYClsWu2nQ6w3NMsFAW','user',1,'2026-05-08 07:59:54.118402',NULL),(24,'Dummy User 23','user23','$2a$11$sln64YD7kbC2jlmX7byBSemp/nKjPUrPsuh5qqsu3/PyTc71dj3VK','user',1,'2026-05-08 07:59:54.250158',NULL),(25,'Dummy User 24','user24','$2a$11$4VxgmmMuKHjQnG05./WONO1Ig19B4oIiXwpU1NF2tP2fjOUxsNwGK','user',1,'2026-05-08 07:59:54.403294',NULL),(26,'Dummy User 25','user25','$2a$11$eIjGOsHVZb1Y743dWHu.G.cKyI8yDtuFRIB..5KY9t4ui0ZHNwyaC','user',1,'2026-05-08 07:59:54.533837',NULL),(27,'Dummy User 26','user26','$2a$11$4OmWGW2UVRpE56syHAQ21.0r71VWk0YIg3bWTBSsPmFCdeRmXjKw2','user',1,'2026-05-08 07:59:54.655769',NULL),(28,'Dummy User 27','user27','$2a$11$I.XD1vqqnXvk/dZQaMfuAugsU6PK3zChlbnmHHmX72dKncikdQ6/y','user',1,'2026-05-08 07:59:54.785121',NULL),(29,'Dummy User 28','user28','$2a$11$foA1YRiMjVDMuChZetoFsedbaf0gfF/CuHzMsnWi7ILjH1qnSg8JS','user',1,'2026-05-08 07:59:54.916685',NULL),(30,'Dummy User 29','user29','$2a$11$OHIBEnZRMDIOa4xMeImxqeIgIXJi81AwY1laGzPBM27H.S41p.PGq','user',1,'2026-05-08 07:59:55.049541',NULL),(31,'Dummy User 30','user30','$2a$11$L2eb0XP/pt0s3aGy/IFenO0ALopGFLTfMA6CJ0Fel/8fwThNgRVye','user',1,'2026-05-08 07:59:55.193078',NULL),(32,'Dummy User 31','user31','$2a$11$C6y.CCnXxO79gMWRasjFVuJzNkNZrGb3eXFcmOdL3SAxaSIuM3zb6','user',1,'2026-05-08 07:59:55.340251',NULL),(33,'Dummy User 32','user32','$2a$11$0rzVxrGLi7Qcohonw6R4M.xZFHT5ZJPDg0.u8EHTpbZGYfzpvHEby','user',1,'2026-05-08 07:59:55.483930',NULL),(34,'Dummy User 33','user33','$2a$11$yqvzVZMuyCnjlcpshnT3me8bL55u/KwfPakrCDmmdnYniF50.rhNm','user',1,'2026-05-08 07:59:55.624382',NULL),(35,'Dummy User 34','user34','$2a$11$LOLgVo2bLSIj75B8ETD46.GAz5M9zdaCre2lNUH86uQWOpvNO.weS','user',1,'2026-05-08 07:59:55.770335',NULL),(36,'Dummy User 35','user35','$2a$11$jQKXUonRKdjm8VNEnTLFcuxB27PYJ8Gv/njtSu1dWzGArEVCHyVeS','user',1,'2026-05-08 07:59:55.901126',NULL),(37,'Dummy User 36','user36','$2a$11$SzkXYazX.I5GBNcg5c0QY.A/LhaUyQL66IqIGBE1V/I.s0rymSxiW','user',1,'2026-05-08 07:59:56.037310',NULL),(38,'Dummy User 37','user37','$2a$11$rsIhvMJAW7YH7dzzP.wjxOSg78jeiyqiVX9eL7i41YF2czYXscCZ2','user',1,'2026-05-08 07:59:56.160479',NULL),(39,'Dummy User 38','user38','$2a$11$jGLJV35Z89pzMycRMPUsu.YaHi2WHAQtwN5g6vZjCmHJMbD1tIz.a','user',1,'2026-05-08 07:59:56.289465',NULL),(40,'Dummy User 39','user39','$2a$11$.w85ctKZHhJDJXHneg./gOfqxflRS7VHIST7NiooF72OFzKuMek.u','user',1,'2026-05-08 07:59:56.446314',NULL),(41,'Dummy User 40','user40','$2a$11$1tvdEHwxq8TUkg9re0VkseTplhYzg1Rb/PJK2ldvFz/fqXwt9MMC6','user',1,'2026-05-08 07:59:56.572381',NULL),(42,'Dummy User 41','user41','$2a$11$FLDiiOSUinQy7AN6UWIyfO7jkPon.2Chn62Bq.YsUZ65B6TzwWzku','user',1,'2026-05-08 07:59:56.705606',NULL),(43,'Dummy User 42','user42','$2a$11$XqEmSIRhV4LziryWseXHve5hs6BAfIgjNVWVKZnbgoUo95OD0qDY2','user',1,'2026-05-08 07:59:56.833099',NULL),(44,'Dummy User 43','user43','$2a$11$4UwHabkeuFvaWRNFCCgD.O3gI9eapm1p9XYoiruF7jCmtROPtFSSS','user',1,'2026-05-08 07:59:56.967866',NULL),(45,'Dummy User 44','user44','$2a$11$kQ.Xw1h2zoOt6jcSw.YHKe65iD1TSiQGSyW1C1MTdb4JxODQVPlJy','user',1,'2026-05-08 07:59:57.091622',NULL),(46,'Dummy User 45','user45','$2a$11$J0InbU/yW5nWqSGHJMar9.EE8yDBNDuiqwva6dbsYu4ZGMjDgK9SW','user',1,'2026-05-08 07:59:57.219795',NULL),(47,'Dummy User 46','user46','$2a$11$HuHJfepJVmU8ljdhZY4J9.tNaC/tgRMobav3UDq2x0cKuLR8QqMQu','user',1,'2026-05-08 07:59:57.343955',NULL),(48,'Dummy User 47','user47','$2a$11$6X4v/Q0JY8zRuuGOKIt/Vuiyz/Oteo57uAdesWOxHmzKOkbRmDdly','user',1,'2026-05-08 07:59:57.468759',NULL),(49,'Dummy User 48','user48','$2a$11$81Fcyydjjk2KL/qByTFGdeXqQ8VRr/ePTS0yIDwnPTEtonWWPpHWm','user',1,'2026-05-08 07:59:57.595285',NULL),(50,'Dummy User 49','user49','$2a$11$5kLtNA2PFXyL7Ekc2FCs2.rD/O3x40nRm0N.PAiNuVspStSd9J/B.','user',1,'2026-05-08 07:59:57.744000',NULL),(51,'Dummy User 50','user50','$2a$11$XlHlyyOWxlQ9nWN6C0FjG.efRQ4OUslPLcJ0CpUDIZzmhHkhOuxHi','user',1,'2026-05-08 07:59:57.871642',NULL),(52,'Dummy User 51','user51','$2a$11$fb/3Xsz9gOInTeH9IZrRD.OpKCm8YM9BRV7jt2lx33bPfJxZdyx0O','user',1,'2026-05-08 07:59:57.997229',NULL),(53,'Dummy User 52','user52','$2a$11$bD/SmqCiHKwgXFDlQNgZ/OWrNGoosc7GsMRg83FJ3mTzLcEhpJFCy','user',1,'2026-05-08 07:59:58.156958',NULL),(54,'Dummy User 53','user53','$2a$11$jhj/aZvYM5BiRWGNxN3MtuNefrg9VT0UgUqlGTgGlgroiwktVs75u','user',1,'2026-05-08 07:59:58.281887',NULL),(55,'Dummy User 54','user54','$2a$11$ZoKe1GLZdEgOoD7BHB61.Ow2tvaJ29TXjklBCnHEIKqXoPmxtIgJS','user',1,'2026-05-08 07:59:58.407604',NULL),(56,'Dummy User 55','user55','$2a$11$gU0bVzTuKJ.sIaUftApzVuVyuNIxjm08f9X3CdNZxhRWfKrM3pf3y','user',1,'2026-05-08 07:59:58.539236',NULL),(57,'Dummy User 56','user56','$2a$11$t6BDITqozVXqX4OMyB8deeoMcUT/kzli.Tsu9oRHbwNosdLc3N48S','user',1,'2026-05-08 07:59:58.667487',NULL),(58,'Dummy User 57','user57','$2a$11$JozINI/XexwbA7CuZHgIEedlEtowL4Wno9zGwtAzCcu55qX4ZtI4q','user',1,'2026-05-08 07:59:58.796443',NULL),(59,'Dummy User 58','user58','$2a$11$JVYw7gepgtfs/tuuMnfAKuDMz8q7sqBnEfUdAQWmlc0bZJiNgXGKG','user',1,'2026-05-08 07:59:58.929082',NULL),(60,'Dummy User 59','user59','$2a$11$GqV.gYxK4gg8Hl3vUk8VK.C1ESuvDPn.7BP/eqS1b065bY8jPlMFO','user',1,'2026-05-08 07:59:59.083858',NULL),(61,'Dummy User 60','user60','$2a$11$gVyBqeNsT5ErI/jFLHezRuIYQ4leypAj1wGQxSZju6dIVri.uwhTW','user',1,'2026-05-08 07:59:59.225889',NULL),(62,'Dummy User 61','user61','$2a$11$6Vh0JK6UkNc76Nvmp038qeK7Sz4bS259nZ7Kz9Wpn.T.uusnPTmPW','user',1,'2026-05-08 07:59:59.357890',NULL),(63,'Dummy User 62','user62','$2a$11$bKZLItEIoAd0TuDtavd6ae7M2nDrh3Syt8r44PWWpPt6242EeY6om','user',1,'2026-05-08 07:59:59.490903',NULL),(64,'Dummy User 63','user63','$2a$11$FpNH.Z8SWrMoW4qqGCb3F.CfhDq7BIC69JlNG1Y6jEydx0u1MsRWq','user',1,'2026-05-08 07:59:59.649325',NULL),(65,'Dummy User 64','user64','$2a$11$xm7/9fwq1lz17Kf9U.oHvua9c46jZCuFqQpjiQkfI1l0S1kCpKlOa','user',1,'2026-05-08 07:59:59.797755',NULL),(66,'Dummy User 65','user65','$2a$11$lobJzbhRP5aoX7LuIzfSHOd8H5A2EPNS0joR2690tGf.5RS7ACtAi','user',1,'2026-05-08 07:59:59.936950',NULL),(67,'Dummy User 66','user66','$2a$11$Y8f4zRxw3aVWstnG02Lzfe5bIbkv3YiunOh22.dpFivOQ2po/BecW','user',1,'2026-05-08 08:00:00.068247',NULL),(68,'Dummy User 67','user67','$2a$11$BlSmsDNXS7DY0UxrNYyypuzdeS5YK6rHVlMckAmt357vai/Pi3cAO','user',1,'2026-05-08 08:00:00.193008',NULL),(69,'Dummy User 68','user68','$2a$11$U47HihaGEhgBga3sUn.j5eDkxRmIllm8YPa71aipMqCozmnnbZSUK','user',1,'2026-05-08 08:00:00.316495',NULL),(70,'Dummy User 69','user69','$2a$11$6FNbGeG8xMiDlTwZjn440ejuwvZ6iAubXsHnYVRuj6cmmkVACCL8m','user',1,'2026-05-08 08:00:00.450696',NULL),(71,'Dummy User 70','user70','$2a$11$wd6T2zN/sqz0V.WG528PWu05ib5bxm6pbdpZgvGaGAZsTecZ5COHW','user',1,'2026-05-08 08:00:00.587152',NULL),(72,'Dummy User 71','user71','$2a$11$mmIUwH0PXTHChR936FzKi.WP6aClqvkYMCu4OmkkPvHx14ixIefvW','user',1,'2026-05-08 08:00:00.718995',NULL),(73,'Dummy User 72','user72','$2a$11$3XZnkZ7j9mr4NiG3DqPn9.UNqAl8gSCnoYL6RSfUlrSIV0A1a/mj6','user',1,'2026-05-08 08:00:00.846408',NULL),(74,'Dummy User 73','user73','$2a$11$QaAxJRaeXTCV7AcnHsbPrOs1idUnH8fpMd1Hbad8MK7TTu9FPTOie','user',1,'2026-05-08 08:00:00.979174',NULL),(75,'Dummy User 74','user74','$2a$11$AIxonPmp9Tndi9iIXZ9BP.t4n2dthbLNkdNyQVk/JgxVW.6XZhxUG','user',1,'2026-05-08 08:00:01.106131',NULL),(76,'Dummy User 75','user75','$2a$11$x4UiB5dExT0gNzJlGjVS6Ohk6zLMVcs10wx9YqoKTi5uEgWrbp5aO','user',1,'2026-05-08 08:00:01.230548',NULL),(77,'Dummy User 76','user76','$2a$11$xz2t1tcORUDhvUVoYir5ou3T12ezguwla9vRRlfcB0NtXUsK3cydq','user',1,'2026-05-08 08:00:01.358176',NULL),(78,'Dummy User 77','user77','$2a$11$8U2L/8IKdwxYtyIBe0h7WuEhRnPP58ijia9bykhiJw//Mz7B30F4C','user',1,'2026-05-08 08:00:01.489206',NULL),(79,'Dummy User 78','user78','$2a$11$nOaYvfx94s098QO8y76SUeqUCf6LeUDYRaDI.naDo4mUlyStjwd5m','user',1,'2026-05-08 08:00:01.628849',NULL),(80,'Dummy User 79','user79','$2a$11$Pykhr06wCe6xn1nsHxgcv.aTtQWhkhn35r/3Mnprrx6jGEPp0pht6','user',1,'2026-05-08 08:00:01.763479',NULL),(81,'Dummy User 80','user80','$2a$11$042a6kY7YYj4SGqwgmI3pejyqmy1eyb/AGHPx9ps41TWC9JFVpi3a','user',1,'2026-05-08 08:00:01.893886',NULL),(82,'Dummy User 81','user81','$2a$11$U.kgU1uG5K29idnE/T4yTOGiX3a0fGNa79rTDMyV4M1/eRurq9sWW','user',1,'2026-05-08 08:00:02.040453',NULL),(83,'Dummy User 82','user82','$2a$11$uPQ83If6fGaIzHCZ49nRlOUvl0rPraquAzGOQvg2GEagM3mtMulOe','user',1,'2026-05-08 08:00:02.173376',NULL),(84,'Dummy User 83','user83','$2a$11$6MU02O67TmsjpbrbQD3s6upy89VFyGNayfWLFY52YbqBtw9voV77u','user',1,'2026-05-08 08:00:02.302645',NULL),(85,'Dummy User 84','user84','$2a$11$yrRZtLKyQAGUIo.U3wN3TeJi2e/MInITHr/nUxwnq7lgQhdrGcvy6','user',1,'2026-05-08 08:00:02.432540',NULL),(86,'Dummy User 85','user85','$2a$11$b.O5kSTBOa/rpEbMN25kduljhXH/o.Jz7OGxi.yiXpo8yD.L9W/Ia','user',1,'2026-05-08 08:00:02.559103',NULL),(87,'Dummy User 86','user86','$2a$11$pv2kIMkNgaN/Xevs2JtDy.nQffpUX8ZZbaXIXNImai/Iu5uBgRGMG','user',1,'2026-05-08 08:00:02.684012',NULL),(88,'Dummy User 87','user87','$2a$11$mvwqLdSkMXA5xPWmupTnn.uz4v9/9zm/VNSaUTjhfoHveE5YJ6zjK','user',1,'2026-05-08 08:00:02.810350',NULL),(89,'Dummy User 88','user88','$2a$11$bA3UR2FNm6906i2TXiW96.Z9o6udVEkeWLBh1My0vDH9d/xdRa6t2','user',1,'2026-05-08 08:00:02.941225',NULL),(90,'Dummy User 89','user89','$2a$11$R6LZwHohTE4WHo2/7jSyqumfk5EaPeakuSW5BsMHT6m5scxb22MJK','user',1,'2026-05-08 08:00:03.069152',NULL),(91,'Dummy User 90','user90','$2a$11$SyZ4Ow1rTjwP8B.FzgPTpOpRUDci2.NDDfU9LCYavLmGfv2ny/yg.','user',1,'2026-05-08 08:00:03.201367',NULL),(92,'Dummy User 91','user91','$2a$11$V6jAXxpRTFCW2gFot270Ze5AHV0jB.WJDt9yYWVqbBTycZeFhh0LW','user',1,'2026-05-08 08:00:03.336286',NULL),(93,'Dummy User 92','user92','$2a$11$G.tu906JhHxVxsLyvtnn3ur0p5hak8bnQ0Z.KycS.olcxRsIRpIJu','user',1,'2026-05-08 08:00:03.471459',NULL),(94,'Dummy User 93','user93','$2a$11$eFMwzu1iw6nSSHmlGlNVKOMg6o8j4vXAdpOLWU96myA.N6IiyJj/.','user',1,'2026-05-08 08:00:03.615532',NULL),(95,'Dummy User 94','user94','$2a$11$pIOGBjN0nS8vyUQrjGUSDOlbU9jHri.QV7MPMSUlAQV2ArFSdlfgm','user',1,'2026-05-08 08:00:03.759043',NULL),(96,'Dummy User 95','user95','$2a$11$49la8gqF0gEgwqm3IKgN/u5l3IqFdwFVDnxfu2p5ooIhvj/KOpqBK','user',1,'2026-05-08 08:00:03.901973',NULL),(97,'Dummy User 96','user96','$2a$11$iDMYbGp93l9C0M7e3V7YKezYSDbAh2BOmNXAalwFDvGCmRoNvicpa','user',1,'2026-05-08 08:00:04.053409',NULL),(98,'Dummy User 97','user97','$2a$11$cTH6OYEFavA6xq7bv6z1FOw2aF3WxPNmq3zU7joJI8nuAoS/9Fwgm','user',1,'2026-05-08 08:00:04.200698',NULL),(102,'User','user','$2a$11$pdmq1mpI6/PZHIDCZhidSuhFxqNhbqKlDb6YVWOvJz.PMp2J1kQDi','user',1,'2026-06-10 03:36:28.276194',NULL);
/*!40000 ALTER TABLE `users` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Dumping routines for database 'trss_traceability_system'
--
/*!50003 DROP PROCEDURE IF EXISTS `sp_insert_trace_data` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_insert_trace_data`(
    IN p_issue_no VARCHAR(100),
    IN p_is_ok TINYINT(1),
    IN p_process_code VARCHAR(50),
    IN p_details_json JSON
)
BEGIN
    DECLARE v_log_id BIGINT;
    DECLARE v_process_id INT;
    DECLARE v_final_issue_no VARCHAR(110);
    DECLARE i INT DEFAULT 0;
    DECLARE v_count INT;
    
    -- 1. Penentuan Suffix -R
    IF p_is_ok = 0 THEN
        SET v_final_issue_no = CONCAT(p_issue_no, '-R');
    ELSE
        SET v_final_issue_no = p_issue_no;
    END IF;

    -- 2. Lookup Process ID
    SELECT id INTO v_process_id FROM processes WHERE code = p_process_code LIMIT 1;
    IF v_process_id IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Process Code not found';
    END IF;

    START TRANSACTION;

    -- 3. LOGIC UPSERT HEADER:
    -- Cari apakah sudah ada header dengan IssueNo ini yang masih aktif
    SELECT id INTO v_log_id FROM process_logs WHERE issue_no = v_final_issue_no AND is_active = 1 LIMIT 1;

    IF v_log_id IS NULL THEN
        -- Kalau belum ada, baru buat header baru
        INSERT INTO process_logs (issue_no, is_active, created_at)
        VALUES (v_final_issue_no, 1, NOW());
        SET v_log_id = LAST_INSERT_ID();
    ELSE
        -- Kalau sudah ada, opsional: update waktu terakhir diupdate
        UPDATE process_logs SET updated_at = NOW() WHERE id = v_log_id;
    END IF;

    -- 4. Loop Insert Detail (Tetap nambah baris baru untuk setiap proses)
    SET v_count = JSON_LENGTH(p_details_json);
    WHILE i < v_count DO
        INSERT INTO process_log_details (
            process_log_id, 
            process_id, 
            parameter_id, 
            value_number, 
            value_text, 
            value_boolean, 
            created_at
        )
        SELECT 
            v_log_id,
            v_process_id,
            p.id,
            NULLIF(JSON_UNQUOTE(JSON_EXTRACT(p_details_json, CONCAT('$[', i, '].val_num'))), 'null'),
            NULLIF(JSON_UNQUOTE(JSON_EXTRACT(p_details_json, CONCAT('$[', i, '].val_txt'))), 'null'),
            NULLIF(JSON_UNQUOTE(JSON_EXTRACT(p_details_json, CONCAT('$[', i, '].val_bool'))), 'null'),
            NOW()
        FROM parameters p 
        WHERE p.code = JSON_UNQUOTE(JSON_EXTRACT(p_details_json, CONCAT('$[', i, '].parameter_code')))
        LIMIT 1;

        SET i = i + 1;
    END WHILE;

    COMMIT;

    SELECT v_log_id AS log_id;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-06-18 18:30:15
