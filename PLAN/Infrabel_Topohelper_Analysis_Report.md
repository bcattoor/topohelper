# Plan for COGO Point Automated Naming Convention

This document outlines the development plan for creating a robust, automated, and configurable naming convention system for COGO points within the TopoHelper application.

## 1. Project Goals

- **Automated Naming:** Replace the manual naming process with an automated system that generates names based on configurable rules.
- **Prefix Prioritization:** Implement a primary rule to handle points with descriptions starting with "CATA" and "CATB".
- **Dynamic Generation:** Allow users to define naming patterns using point attributes like Northing, Easting, Elevation, and Description.
- **Configurability:** Provide a user interface for managing naming patterns and lookup tables.
- **Compliance and Uniqueness:** Ensure all generated names are unique and comply with AutoCAD standards.
- **Integration:** Seamlessly integrate the new naming engine into the existing "From Block to Cogo" command.

## 2. Development Phases

The project will be broken down into three main phases:

---

### **Phase 1: Analysis and Solution Design**

This phase focuses on analyzing the current implementation and designing a flexible architecture for the new naming engine.

- **Step 1.1: Analyze Existing Code (`FromBlockToCogo.cs`)**
  - Review the current name generation logic, which relies on the `GetPuntNummer` function and a simple suffix-based uniqueness check.
  - Analyze the description assignment logic (classification and last-digit fallback), as this will be a key input for the new naming system.

- **Step 1.2: Design the Naming Rule Engine**
  - Define a clear, prioritized logic flow:
    1.  Determine the point's description first.
    2.  Check for a "CATA" or "CATB" prefix in the description and apply a specific pattern.
    3.  If no prefix match, consult a user-defined lookup table (e.g., Description "TREE" -> Prefix "TR").
    4.  If no match in the lookup table, apply a user-defined default pattern.
    5.  Construct the point name using the selected pattern and the point's attributes.
    6.  Validate the name for length and allowed characters.
    7.  Guarantee uniqueness by appending a numeric counter if a conflict exists.

- **Step 1.3: Design the Configuration Model**
  - Create a new data model (e.g., a `CogoPointNamingSettings` class) to store the user's configuration. This will include:
    - `PrefixPatterns`: A dictionary to hold specific patterns for "CATA" and "CATB".
    - `DescriptionLookupTable`: A list or dictionary for mapping other descriptions to specific prefixes or patterns.
    - `DefaultPattern`: A fallback pattern to use when no other rules apply.
  - This model will be designed for easy serialization to XML to be stored in the application settings.

---

### **Phase 2: Implementation**

This phase involves writing the code for the new system.

- **Step 2.1: Implement the Configuration UI**
  - Extend the `SettingsUserControl.xaml` to include a new section for "COGO Point Naming".
  - Add UI elements (text boxes, grids) to allow users to easily define the naming patterns and the description lookup table.
  - Implement the logic in `SettingsUserControl.xaml.cs` to load and save the `CogoPointNamingSettings` from/to the application's XML settings.

- **Step 2.2: Build the Naming Engine**
  - Create a dedicated class (e.g., `CogoPointNamer`) to encapsulate all the naming logic designed in Phase 1.
  - This class will accept a CogoPoint object, the naming configuration, and the set of all existing point names.
  - It will contain methods to parse the patterns (e.g., replacing `{Easting:5}` with the first five digits of the Easting coordinate) and return a final, unique point name.

- **Step 2.3: Integrate the Engine into the Command**
  - Modify the `ExecuteCommand` method in `FromBlockToCogo.cs`.
  - Load the naming configuration at the start of the command.
  - In the main processing loop, after a point's description is determined, call the new `CogoPointNamer` to generate the `PointName`.
  - Replace the old `GetPuntNummer` logic with the call to the new engine.

---

### **Phase 3: Testing & Documentation**

This final phase ensures the new system is reliable and well-documented.

- **Step 3.1: Unit Testing**
  - Develop a suite of unit tests for the `CogoPointNamer` class to verify all rule-processing scenarios, including pattern parsing, prefix matching, lookup table logic, and uniqueness enforcement.

- **Step 3.2: End-to-End Testing**
  - Perform manual testing within the AutoCAD environment.
  - Test with various block types and configurations.
  - Verify that the UI for settings works correctly and that the generated names appear as expected in the drawing and the final conversion report.

- **Step 3.3: Documentation**
  - Update code comments to reflect the new architecture.
  - Create a brief user guide explaining how to use the new Naming Convention configuration panel.
  - Update this plan with any changes made during development.