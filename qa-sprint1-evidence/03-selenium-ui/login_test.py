from selenium import webdriver
from selenium.webdriver.common.by import By
import time

driver = webdriver.Chrome()
driver.get("http://localhost:5173")
time.sleep(2) # Give the page a moment to fully load

# Test 1: Enter wrong password 5 times to trigger a security lockout
for i in range(5):
    print(f"Executing login attempt {i+1}...")

    driver.find_element(By.NAME, "identifier").clear()
    driver.find_element(By.NAME, "identifier").send_keys("alice@hms.lk")
    time.sleep(0.5) # Pauses so you can see it type the email

    driver.find_element(By.NAME, "password").clear()
    driver.find_element(By.NAME, "password").send_keys("wrongpassword")
    time.sleep(0.5) # Pauses so you can see it type the password

    driver.find_element(By.CSS_SELECTOR, "button[type='submit']").click()
    time.sleep(2) # Increased to 2 seconds so you can watch the button click and any error message appear

print("Test complete! Printing first 500 characters of final page source:")
print(driver.page_source[:500])

time.sleep(5) # Keeps the browser window open for 5 seconds at the end before closing
driver.quit()



'''from selenium import webdriver
from selenium.webdriver.common.by import By
import time

# Start Chrome Browser
driver = webdriver.Chrome()
driver.get("http://localhost:5173")
time.sleep(2)

# Test 1: Enter wrong password 5 times to trigger a security lockout
for i in range(5):
    # CHANGED: Using "identifier" to match your actual HTML form name
    driver.find_element(By.NAME, "identifier").clear()
    driver.find_element(By.NAME, "identifier").send_keys("alice@hms.lk")

    # Kept as password (double check your inspect panel if this errors out)
    driver.find_element(By.NAME, "password").clear()
    driver.find_element(By.NAME, "password").send_keys("wrongpassword")

    # Locates your "Sign In" button and clicks it
    driver.find_element(By.CSS_SELECTOR, "button[type='submit']").click()
    time.sleep(1)

print("After 5 failed attempts, page shows:", driver.page_source[:500])
time.sleep(3)
driver.quit()
'''