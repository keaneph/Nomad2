-- Test Case 1: Try to insert a customer with invalid status
INSERT INTO customer (customer_id, name, phone_number, address, government_id_picture, customer_status, registration_date)
VALUES ('C00000001', 'Test Customer', '1234567890', 'Test Address', 'test.jpg', 'InvalidStatus', '2024-03-20');

-- Test Case 2: Try to insert a bike with negative daily rate
INSERT INTO bike (bike_id, bike_model, bike_type, daily_rate, bike_picture, bike_status)
VALUES ('B00000001', 'Test Bike', 'Mountain', -100, 'test.jpg', 'Available');

-- Test Case 3: Try to insert a rental with non-existent customer and bike
INSERT INTO rentals (rental_id, customer_id, bike_id, rental_date, rental_status)
VALUES ('R00000001', 'NONEXISTENT', 'NONEXISTENT', '2024-03-20', 'Active');

-- Test Case 4: Try to insert duplicate phone number
INSERT INTO customer (customer_id, name, phone_number, address, government_id_picture, customer_status, registration_date)
VALUES ('C00000002', 'Test Customer 2', '1234567890', 'Test Address 2', 'test2.jpg', 'Active', '2024-03-20');

-- Test Case 5: Try to delete a customer with active rentals
-- First create a customer and bike
INSERT INTO customer (customer_id, name, phone_number, address, government_id_picture, customer_status, registration_date)
VALUES ('C00000003', 'Test Customer 3', '9876543210', 'Test Address 3', 'test3.jpg', 'Active', '2024-03-20');

INSERT INTO bike (bike_id, bike_model, bike_type, daily_rate, bike_picture, bike_status)
VALUES ('B00000002', 'Test Bike 2', 'Road', 100, 'test2.jpg', 'Available');

-- Create an active rental
INSERT INTO rentals (rental_id, customer_id, bike_id, rental_date, rental_status)
VALUES ('R00000002', 'C00000003', 'B00000002', '2024-03-20', 'Active');

-- Try to delete the customer
DELETE FROM customer WHERE customer_id = 'C00000003';

-- Test Case 6: Try to set bike status to Available while it has active rental
UPDATE bike SET bike_status = 'Available' WHERE bike_id = 'B00000002';

-- Test Case 7: Try to delete a bike with active rental
DELETE FROM bike WHERE bike_id = 'B00000002';

-- Clean up test data
DELETE FROM rentals WHERE rental_id = 'R00000002';
DELETE FROM bike WHERE bike_id = 'B00000002';
DELETE FROM customer WHERE customer_id = 'C00000003'; 