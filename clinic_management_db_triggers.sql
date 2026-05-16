-- MySQL dump 10.13  Distrib 8.0.45, for Win64 (x86_64)
--
-- Host: 127.0.0.1    Database: clinic_management_db
-- ------------------------------------------------------
-- Server version	8.0.45

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Temporary view structure for view `appointment_report`
--

DROP TABLE IF EXISTS `appointment_report`;
/*!50001 DROP VIEW IF EXISTS `appointment_report`*/;
SET @saved_cs_client     = @@character_set_client;
/*!50503 SET character_set_client = utf8mb4 */;
/*!50001 CREATE VIEW `appointment_report` AS SELECT 
 1 AS `appointment_id`,
 1 AS `patient_first`,
 1 AS `patient_last`,
 1 AS `doctor_first`,
 1 AS `doctor_last`,
 1 AS `appointment_date`,
 1 AS `status`*/;
SET character_set_client = @saved_cs_client;

--
-- Table structure for table `appointments`
--

DROP TABLE IF EXISTS `appointments`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `appointments` (
  `appointment_id` int NOT NULL AUTO_INCREMENT,
  `patient_id` int NOT NULL,
  `doctor_id` int NOT NULL,
  `appointment_date` datetime NOT NULL,
  `status` enum('Scheduled','Completed','Cancelled') DEFAULT 'Scheduled',
  PRIMARY KEY (`appointment_id`),
  KEY `patient_id` (`patient_id`),
  KEY `doctor_id` (`doctor_id`),
  CONSTRAINT `appointments_ibfk_1` FOREIGN KEY (`patient_id`) REFERENCES `patients` (`patient_id`) ON DELETE CASCADE,
  CONSTRAINT `appointments_ibfk_2` FOREIGN KEY (`doctor_id`) REFERENCES `doctors` (`doctor_id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=12 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `appointments`
--

LOCK TABLES `appointments` WRITE;
/*!40000 ALTER TABLE `appointments` DISABLE KEYS */;
INSERT INTO `appointments` VALUES (1,1,1,'2026-03-01 09:00:00','Scheduled'),(2,2,2,'2026-03-01 10:00:00','Scheduled'),(3,3,3,'2026-03-01 11:00:00','Scheduled'),(4,4,4,'2026-03-02 09:00:00','Scheduled'),(5,5,5,'2026-03-02 10:30:00','Scheduled'),(6,6,6,'2026-03-02 11:30:00','Scheduled'),(7,7,7,'2026-03-03 09:15:00','Scheduled'),(8,8,8,'2026-03-03 10:45:00','Scheduled'),(9,9,9,'2026-03-03 11:30:00','Scheduled'),(10,10,10,'2026-03-04 09:00:00','Scheduled'),(11,1,1,'2026-03-07 15:50:50','Completed');
/*!40000 ALTER TABLE `appointments` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
-- DISCUSSION: Pagka-insert ng bagong appointment, automatic na gagawa ng record 
-- sa treatments table para sa 'Standard Consultation' na nagkakahalaga ng 500.00.
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `after_appointment_insert` AFTER INSERT ON `appointments` FOR EACH ROW BEGIN
    INSERT INTO treatments (appointment_id, description, cost)
    VALUES (NEW.appointment_id, 'Standard Consultation', 500.00);
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
-- DISCUSSION: Kapag binago ang status ng appointment tungo sa 'Completed', 
-- tatawagin ng trigger ang function na 'get_total_treatment_cost' para 
-- automatic na mag-record ng bayad sa payments table.
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `after_appointment_update` AFTER UPDATE ON `appointments` FOR EACH ROW BEGIN
    IF NEW.status = 'Completed' AND OLD.status <> 'Completed' THEN
        INSERT INTO payments (appointment_id, amount_paid, payment_method)
        VALUES (NEW.appointment_id, get_total_treatment_cost(NEW.appointment_id), 'Cash');
    END IF;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `doctors`
--

DROP TABLE IF EXISTS `doctors`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `doctors` (
  `doctor_id` int NOT NULL AUTO_INCREMENT,
  `first_name` varchar(50) NOT NULL,
  `last_name` varchar(50) NOT NULL,
  `specialization` varchar(100) NOT NULL,
  `phone` varchar(15) DEFAULT NULL,
  `email` varchar(100) DEFAULT NULL,
  PRIMARY KEY (`doctor_id`),
  UNIQUE KEY `phone` (`phone`),
  UNIQUE KEY `email` (`email`)
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `doctors`
--

LOCK TABLES `doctors` WRITE;
/*!40000 ALTER TABLE `doctors` DISABLE KEYS */;
INSERT INTO `doctors` VALUES (1,'Dr. Roberto','Pascual','Pediatrics','09111110001','roberto@gmail.com'),(2,'Dr. Elena','Villanueva','Dermatology','09111110002','elena@gmail.com'),(3,'Dr. Ricardo','Dizon','Internal Medicine','09111110003','ricardo@gmail.com'),(4,'Dr. Angela','Soriano','OB-GYN','09111110004','angela@gmail.com'),(5,'Dr. Fernando','Mendoza','Cardiology','09111110005','fernando@gmail.com'),(6,'Dr. Sofia','Bautista','ENT','09111110006','sofia@gmail.com'),(7,'Dr. Gabriel','Quizon','Neurology','09111110007','gabriel@gmail.com'),(8,'Dr. Beatrice','Yee','Orthopedics','09111110008','beatrice@gmail.com'),(9,'Dr. Manuel','Castro','Family Medicine','09111110009','manuel@gmail.com'),(10,'Dr. Teresa','Ocampo','Psychiatry','09111110010','teresa@gmail.com');
/*!40000 ALTER TABLE `doctors` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
-- DISCUSSION: Ito ay safety feature na humaharang sa pag-delete ng Doctor record 
-- kung ang nasabing doctor ay may mga naka-schedule o past appointments pa.
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `before_doctor_delete` BEFORE DELETE ON `doctors` FOR EACH ROW BEGIN
    -- This prevents deleting a doctor if they have appointments scheduled
    DECLARE appointment_count INT;
    SELECT COUNT(*) INTO appointment_count FROM appointments WHERE doctor_id = OLD.doctor_id;
    
    IF appointment_count > 0 THEN
        SIGNAL SQLSTATE '45000' 
        SET MESSAGE_TEXT = 'Cannot delete doctor: Active appointments exist in the system.';
    END IF;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `patients`
--

DROP TABLE IF EXISTS `patients`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `patients` (
  `patient_id` int NOT NULL AUTO_INCREMENT,
  `first_name` varchar(50) NOT NULL,
  `last_name` varchar(50) NOT NULL,
  `birth_date` date NOT NULL,
  `gender` enum('Male','Female') NOT NULL,
  `phone` varchar(15) DEFAULT NULL,
  `email` varchar(100) DEFAULT NULL,
  PRIMARY KEY (`patient_id`),
  UNIQUE KEY `phone` (`phone`),
  UNIQUE KEY `email` (`email`)
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `patients`
--

LOCK TABLES `patients` WRITE;
/*!40000 ALTER TABLE `patients` DISABLE KEYS */;
INSERT INTO `patients` VALUES (1,'Juan','Dela Cruz','1995-05-10','Male','09111111111','juan@gmail.com'),(2,'Maria','Santos','1998-02-14','Female','09222222222','maria@gmail.com'),(3,'Pedro','Reyes','1990-08-22','Male','09333333333','pedro@gmail.com'),(4,'Ana','Lopez','1992-11-30','Female','09444444444','ana@gmail.com'),(5,'Mark','Lim','1985-01-12','Male','09555555555','mark@gmail.com'),(6,'Liza','Tan','1997-03-17','Female','09666666666','liza@gmail.com'),(7,'Carlo','Gomez','1993-06-19','Male','09777777777','carlo@gmail.com'),(8,'Ella','Torres','1999-09-09','Female','09888888888','ella@gmail.com'),(9,'Ryan','Chua','1988-12-25','Male','09999999999','ryan@gmail.com'),(10,'Nina','Cruz','1994-07-07','Female','09121212121','nina@gmail.com');
/*!40000 ALTER TABLE `patients` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Temporary view structure for view `payment_report`
--

DROP TABLE IF EXISTS `payment_report`;
/*!50001 DROP VIEW IF EXISTS `payment_report`*/;
SET @saved_cs_client     = @@character_set_client;
/*!50503 SET character_set_client = utf8mb4 */;
/*!50001 CREATE VIEW `payment_report` AS SELECT 
 1 AS `payment_id`,
 1 AS `appointment_id`,
 1 AS `patient_first`,
 1 AS `amount_paid`,
 1 AS `payment_method`,
 1 AS `payment_date`*/;
SET character_set_client = @saved_cs_client;

--
-- Table structure for table `payments`
--

DROP TABLE IF EXISTS `payments`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `payments` (
  `payment_id` int NOT NULL AUTO_INCREMENT,
  `appointment_id` int DEFAULT NULL,
  `payment_date` datetime DEFAULT CURRENT_TIMESTAMP,
  `amount_paid` decimal(10,2) NOT NULL,
  `payment_method` enum('Cash','Card','Online') NOT NULL,
  PRIMARY KEY (`payment_id`),
  UNIQUE KEY `appointment_id` (`appointment_id`),
  CONSTRAINT `payments_ibfk_1` FOREIGN KEY (`appointment_id`) REFERENCES `appointments` (`appointment_id`) ON DELETE CASCADE,
  CONSTRAINT `payments_chk_1` CHECK ((`amount_paid` >= 0))
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `payments`
--

LOCK TABLES `payments` WRITE;
/*!40000 ALTER TABLE `payments` DISABLE KEYS */;
INSERT INTO `payments` VALUES (1,11,'2026-03-07 15:54:39',500.00,'Cash');
/*!40000 ALTER TABLE `payments` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Temporary view structure for view `treatment_report`
--

DROP TABLE IF EXISTS `treatment_report`;
/*!50001 DROP VIEW IF EXISTS `treatment_report`*/;
SET @saved_cs_client     = @@character_set_client;
/*!50503 SET character_set_client = utf8mb4 */;
/*!50001 CREATE VIEW `treatment_report` AS SELECT 
 1 AS `treatment_id`,
 1 AS `appointment_id`,
 1 AS `patient_first`,
 1 AS `description`,
 1 AS `cost`*/;
SET character_set_client = @saved_cs_client;

--
-- Table structure for table `treatments`
--

DROP TABLE IF EXISTS `treatments`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `treatments` (
  `treatment_id` int NOT NULL AUTO_INCREMENT,
  `appointment_id` int NOT NULL,
  `description` varchar(255) NOT NULL,
  `cost` decimal(10,2) NOT NULL,
  PRIMARY KEY (`treatment_id`),
  KEY `appointment_id` (`appointment_id`),
  CONSTRAINT `treatments_ibfk_1` FOREIGN KEY (`appointment_id`) REFERENCES `appointments` (`appointment_id`) ON DELETE CASCADE,
  CONSTRAINT `treatments_chk_1` CHECK ((`cost` >= 0))
) ENGINE=InnoDB AUTO_INCREMENT=12 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `treatments`
--

LOCK TABLES `treatments` WRITE;
/*!40000 ALTER TABLE `treatments` DISABLE KEYS */;
INSERT INTO `treatments` VALUES (1,1,'General Checkup',500.00),(2,2,'Skin Treatment',1200.00),(3,3,'Blood Test',300.00),(4,4,'Prenatal Checkup',600.00),(5,5,'Cardiac Checkup',1500.00),(6,6,'Ear Exam',400.00),(7,7,'Neurological Exam',2000.00),(8,8,'Bone X-ray',800.00),(9,9,'Family Medicine Consult',500.00),(10,10,'Psychiatric Evaluation',1000.00),(11,11,'Standard Consultation',500.00);
/*!40000 ALTER TABLE `treatments` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Dumping routines for database 'clinic_management_db'
--
/*!50003 DROP FUNCTION IF EXISTS `get_total_treatment_cost` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` FUNCTION `get_total_treatment_cost`(app_id INT) RETURNS decimal(10,2)
    DETERMINISTIC
BEGIN
    DECLARE total DECIMAL(10,2);
    SELECT SUM(cost) INTO total
    FROM treatments
    WHERE appointment_id = app_id;
    RETURN IFNULL(total,0);
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `mark_appointment_completed` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `mark_appointment_completed`(IN app_id INT)
BEGIN
    UPDATE appointments
    SET status = 'Completed'
    WHERE appointment_id = app_id;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Final view structure for view `appointment_report`
--

/*!50001 DROP VIEW IF EXISTS `appointment_report`*/;
/*!50001 SET @saved_cs_client          = @@character_set_client */;
/*!50001 SET @saved_cs_results         = @@character_set_results */;
/*!50001 SET @saved_col_connection     = @@collation_connection */;
/*!50001 SET character_set_client      = utf8mb4 */;
/*!50001 SET character_set_results     = utf8mb4 */;
/*!50001 SET collation_connection      = utf8mb4_0900_ai_ci */;
/*!50001 CREATE ALGORITHM=UNDEFINED */
/*!50013 DEFINER=`root`@`localhost` SQL SECURITY DEFINER */
/*!50001 VIEW `appointment_report` AS select `a`.`appointment_id` AS `appointment_id`,`p`.`first_name` AS `patient_first`,`p`.`last_name` AS `patient_last`,`d`.`first_name` AS `doctor_first`,`d`.`last_name` AS `doctor_last`,`a`.`appointment_date` AS `appointment_date`,`a`.`status` AS `status` from ((`appointments` `a` join `patients` `p` on((`a`.`patient_id` = `p`.`patient_id`))) join `doctors` `d` on((`a`.`doctor_id` = `d`.`doctor_id`))) */;
/*!50001 SET character_set_client      = @saved_cs_client */;
/*!50001 SET character_set_results     = @saved_cs_results */;
/*!50001 SET collation_connection      = @saved_col_connection */;

--
-- Final view structure for view `payment_report`
--

/*!50001 DROP VIEW IF EXISTS `payment_report`*/;
/*!50001 SET @saved_cs_client          = @@character_set_client */;
/*!50001 SET @saved_cs_results         = @@character_set_results */;
/*!50001 SET @saved_col_connection     = @@collation_connection */;
/*!50001 SET character_set_client      = utf8mb4 */;
/*!50001 SET character_set_results     = utf8mb4 */;
/*!50001 SET collation_connection      = utf8mb4_0900_ai_ci */;
/*!50001 CREATE ALGORITHM=UNDEFINED */
/*!50013 DEFINER=`root`@`localhost` SQL SECURITY DEFINER */
/*!50001 VIEW `payment_report` AS select `pay`.`payment_id` AS `payment_id`,`a`.`appointment_id` AS `appointment_id`,`p`.`first_name` AS `patient_first`,`pay`.`amount_paid` AS `amount_paid`,`pay`.`payment_method` AS `payment_method`,`pay`.`payment_date` AS `payment_date` from ((`payments` `pay` join `appointments` `a` on((`pay`.`appointment_id` = `a`.`appointment_id`))) join `patients` `p` on((`a`.`patient_id` = `p`.`patient_id`))) */;
/*!50001 SET character_set_client      = @saved_cs_client */;
/*!50001 SET character_set_results     = @saved_cs_results */;
/*!50001 SET collation_connection      = @saved_col_connection */;

--
-- Final view structure for view `treatment_report`
--

/*!50001 DROP VIEW IF EXISTS `treatment_report`*/;
/*!50001 SET @saved_cs_client          = @@character_set_client */;
/*!50001 SET @saved_cs_results         = @@character_set_results */;
/*!50001 SET @saved_col_connection     = @@collation_connection */;
/*!50001 SET character_set_client      = utf8mb4 */;
/*!50001 SET character_set_results     = utf8mb4 */;
/*!50001 SET collation_connection      = utf8mb4_0900_ai_ci */;
/*!50001 CREATE ALGORITHM=UNDEFINED */
/*!50013 DEFINER=`root`@`localhost` SQL SECURITY DEFINER */
/*!50001 VIEW `treatment_report` AS select `t`.`treatment_id` AS `treatment_id`,`a`.`appointment_id` AS `appointment_id`,`p`.`first_name` AS `patient_first`,`t`.`description` AS `description`,`t`.`cost` AS `cost` from ((`treatments` `t` join `appointments` `a` on((`t`.`appointment_id` = `a`.`appointment_id`))) join `patients` `p` on((`a`.`patient_id` = `p`.`patient_id`))) */;
/*!50001 SET character_set_client      = @saved_cs_client */;
/*!50001 SET character_set_results     = @saved_cs_results */;
/*!50001 SET collation_connection      = @saved_col_connection */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-03-09 20:40:40
