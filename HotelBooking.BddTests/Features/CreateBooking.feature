Feature: Create booking
  As a hotel guest
  I want to book a room for a period in the future
  So that I am sure to have a room when I arrive

  Background:
    Given the hotel has 3 rooms
    And all rooms are booked from 4 to 18 days from today

  Scenario Outline: Booking a room in relation to the fully occupied period
    When I try to book a room from <start> to <end> days from today
    Then the booking should be <result>

    Examples:
      | tc | classes | start | end | result   |
      | 1  | B-B     | 1     | 3   | created  |
      | 2  | A-A     | 19    | 19  | created  |
      | 3  | B-A     | 3     | 19  | rejected |
      | 4  | B-O     | 3     | 4   | rejected |
      | 5  | B-O     | 3     | 18  | rejected |
      | 6  | O-A     | 4     | 19  | rejected |
      | 7  | O-A     | 18    | 19  | rejected |
      | 8  | O-O     | 4     | 4   | rejected |
      | 9  | O-O     | 4     | 18  | rejected |
      | 10 | O-O     | 18    | 18  | rejected |

  Scenario Outline: Booking with invalid dates
    When I try to book a room from <start> to <end> days from today
    Then the booking should fail because the dates are invalid

    Examples:
      | tc | class   | start | end |
      | 11 | SD<=D   | 0     | 1   |
      | 12 | SD>ED   | 2     | 1   |