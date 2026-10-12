import AppShell from "./AppShell.jsx";

import { useEffect, useMemo, useState } from "react";

import {
  ToolsApiError,
  createFeeInvoice,
  downloadFeeReportCsv,
  getFeeReport,
  getStudentProfiles,
  recordFeePayment
} from "./adminToolsApi.js";
import { getActiveBlocks } from "./roomApi.js";
import { formatDate } from "./leaveFormat.js";

const PAYMENT_METHODS = [
  { value: "CASH", label: "Cash" },
  { value: "CARD", label: "Card" },
  { value: "BANK_TRANSFER", label: "Bank transfer" },
  { value: "ONLINE", label: "Online" }
];

function formatAmount(value) {
  return Number(value).toLocaleString("en-US", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  });
}

function todayInputValue() {
  const now = new Date();
  const month = String(now.getMonth() + 1).padStart(2, "0");
  const day = String(now.getDate()).padStart(2, "0");

  return `${now.getFullYear()}-${month}-${day}`;
}

function errorText(error, fallback) {
  return error instanceof ToolsApiError &&
    error.status !== 401
    ? error.message
    : fallback;
}

/* ---------- Create invoice ---------- */

function InvoiceDialog({ students, onCancel, onSaved }) {
  const [values, setValues] = useState({
    studentProfileId: "",
    feeType: "",
    description: "",
    amount: "",
    dueDate: todayInputValue()
  });
  const [errors, setErrors] = useState({});
  const [serverMessage, setServerMessage] = useState("");
  const [isSaving, setIsSaving] = useState(false);

  function handleChange(event) {
    const { name, value } = event.target;

    setValues((current) => ({ ...current, [name]: value }));
    setErrors((current) => ({ ...current, [name]: "" }));
    setServerMessage("");
  }

  function validate() {
    const found = {};

    if (!values.studentProfileId) {
      found.studentProfileId = "Choose a student.";
    }

    if (values.feeType.trim().length < 2) {
      found.feeType =
        "Fee type must contain at least 2 characters.";
    }

    if (!(Number(values.amount) > 0)) {
      found.amount = "Enter an amount greater than zero.";
    }

    if (!values.dueDate) {
      found.dueDate = "Choose a due date.";
    } else if (values.dueDate < todayInputValue()) {
      found.dueDate = "The due date cannot be in the past.";
    }

    setErrors(found);

    return Object.keys(found).length === 0;
  }

  async function handleSubmit(event) {
    event.preventDefault();

    if (!validate()) {
      return;
    }

    setIsSaving(true);

    try {
      const invoice = await createFeeInvoice({
        studentProfileId: Number(values.studentProfileId),
        feeType: values.feeType.trim(),
        description: values.description.trim() || null,
        amount: Number(values.amount),
        dueDate: values.dueDate
      });

      onSaved(invoice);
    } catch (error) {
      setServerMessage(
        errorText(
          error,
          "Unable to create the invoice. Please try again."
        )
      );
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <div
      className="admin-dialog-backdrop"
      role="presentation"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget && !isSaving) {
          onCancel();
        }
      }}
    >
      <section
        className="admin-confirm-dialog leave-dialog tools-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="invoice-dialog-title"
      >
        <h2 id="invoice-dialog-title">Create fee invoice</h2>

        {serverMessage && (
          <div className="message error" role="alert">
            {serverMessage}
          </div>
        )}

        <form
          className="tools-dialog-form"
          onSubmit={handleSubmit}
          noValidate
        >
          <div className="admin-form-grid">
            <div className="form-group wide">
              <label htmlFor="invoiceStudent">Student</label>

              <select
                id="invoiceStudent"
                name="studentProfileId"
                value={values.studentProfileId}
                onChange={handleChange}
                aria-invalid={Boolean(errors.studentProfileId)}
              >
                <option value="">Select a student</option>
                {students.map((student) => (
                  <option
                    key={student.studentProfileId}
                    value={student.studentProfileId}
                  >
                    {student.registrationNumber} ·{" "}
                    {student.username}
                  </option>
                ))}
              </select>

              {errors.studentProfileId && (
                <p className="field-error">
                  {errors.studentProfileId}
                </p>
              )}
            </div>

            <div className="form-group">
              <label htmlFor="invoiceFeeType">Fee type</label>

              <input
                id="invoiceFeeType"
                name="feeType"
                type="text"
                maxLength={100}
                value={values.feeType}
                onChange={handleChange}
                aria-invalid={Boolean(errors.feeType)}
                placeholder="e.g. Hostel fee"
              />

              {errors.feeType && (
                <p className="field-error">{errors.feeType}</p>
              )}
            </div>

            <div className="form-group">
              <label htmlFor="invoiceAmount">Amount</label>

              <input
                id="invoiceAmount"
                name="amount"
                type="number"
                min="0.01"
                step="0.01"
                value={values.amount}
                onChange={handleChange}
                aria-invalid={Boolean(errors.amount)}
                placeholder="0.00"
              />

              {errors.amount && (
                <p className="field-error">{errors.amount}</p>
              )}
            </div>

            <div className="form-group">
              <label htmlFor="invoiceDueDate">Due date</label>

              <input
                id="invoiceDueDate"
                name="dueDate"
                type="date"
                min={todayInputValue()}
                value={values.dueDate}
                onChange={handleChange}
                aria-invalid={Boolean(errors.dueDate)}
              />

              {errors.dueDate && (
                <p className="field-error">{errors.dueDate}</p>
              )}
            </div>

            <div className="form-group wide">
              <label htmlFor="invoiceDescription">
                Description (optional)
              </label>

              <textarea
                id="invoiceDescription"
                name="description"
                maxLength={500}
                value={values.description}
                onChange={handleChange}
              />
            </div>
          </div>

          <div className="admin-dialog-actions">
            <button
              type="button"
              className="secondary-button"
              disabled={isSaving}
              onClick={onCancel}
            >
              Cancel
            </button>

            <button
              type="submit"
              className="primary-button"
              disabled={isSaving}
            >
              {isSaving ? "Saving..." : "Create invoice"}
            </button>
          </div>
        </form>
      </section>
    </div>
  );
}

/* ---------- Record payment ---------- */

function PaymentDialog({ invoice, onCancel, onSaved }) {
  const [values, setValues] = useState({
    amount: String(invoice.outstandingAmount),
    paymentMethod: "CASH",
    notes: ""
  });
  const [errors, setErrors] = useState({});
  const [serverMessage, setServerMessage] = useState("");
  const [isSaving, setIsSaving] = useState(false);

  function handleChange(event) {
    const { name, value } = event.target;

    setValues((current) => ({ ...current, [name]: value }));
    setErrors((current) => ({ ...current, [name]: "" }));
    setServerMessage("");
  }

  async function handleSubmit(event) {
    event.preventDefault();

    const amount = Number(values.amount);
    const found = {};

    if (!(amount > 0)) {
      found.amount = "Enter an amount greater than zero.";
    } else if (amount > Number(invoice.outstandingAmount)) {
      found.amount =
        "The payment cannot be more than the outstanding amount.";
    }

    setErrors(found);

    if (Object.keys(found).length > 0) {
      return;
    }

    setIsSaving(true);

    try {
      const result = await recordFeePayment(invoice.invoiceId, {
        amount,
        paymentMethod: values.paymentMethod,
        notes: values.notes.trim() || null
      });

      onSaved(result);
    } catch (error) {
      setServerMessage(
        errorText(
          error,
          "Unable to record the payment. Please try again."
        )
      );
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <div
      className="admin-dialog-backdrop"
      role="presentation"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget && !isSaving) {
          onCancel();
        }
      }}
    >
      <section
        className="admin-confirm-dialog leave-dialog tools-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="payment-dialog-title"
      >
        <h2 id="payment-dialog-title">
          Record payment · {invoice.invoiceNumber}
        </h2>

        <p>
          <strong>{invoice.studentName}</strong> ·{" "}
          {invoice.feeType} · outstanding{" "}
          {formatAmount(invoice.outstandingAmount)}
        </p>

        {serverMessage && (
          <div className="message error" role="alert">
            {serverMessage}
          </div>
        )}

        <form
          className="tools-dialog-form"
          onSubmit={handleSubmit}
          noValidate
        >
          <div className="admin-form-grid">
            <div className="form-group">
              <label htmlFor="paymentAmount">Amount</label>

              <input
                id="paymentAmount"
                name="amount"
                type="number"
                min="0.01"
                step="0.01"
                value={values.amount}
                onChange={handleChange}
                aria-invalid={Boolean(errors.amount)}
              />

              {errors.amount && (
                <p className="field-error">{errors.amount}</p>
              )}
            </div>

            <div className="form-group">
              <label htmlFor="paymentMethod">Method</label>

              <select
                id="paymentMethod"
                name="paymentMethod"
                value={values.paymentMethod}
                onChange={handleChange}
              >
                {PAYMENT_METHODS.map((method) => (
                  <option key={method.value} value={method.value}>
                    {method.label}
                  </option>
                ))}
              </select>
            </div>

            <div className="form-group wide">
              <label htmlFor="paymentNotes">
                Notes (optional)
              </label>

              <textarea
                id="paymentNotes"
                name="notes"
                maxLength={500}
                value={values.notes}
                onChange={handleChange}
              />
            </div>
          </div>

          <div className="admin-dialog-actions">
            <button
              type="button"
              className="secondary-button"
              disabled={isSaving}
              onClick={onCancel}
            >
              Cancel
            </button>

            <button
              type="submit"
              className="primary-button"
              disabled={isSaving}
            >
              {isSaving ? "Saving..." : "Record payment"}
            </button>
          </div>
        </form>
      </section>
    </div>
  );
}

/* ---------- Page ---------- */

function AdminFeesPage() {
  const [rows, setRows] = useState([]);
  const [students, setStudents] = useState([]);
  const [blocks, setBlocks] = useState([]);
  const [studentFilter, setStudentFilter] = useState("");
  const [blockFilter, setBlockFilter] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isDownloading, setIsDownloading] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");
  const [showInvoiceDialog, setShowInvoiceDialog] =
    useState(false);
  const [paymentTarget, setPaymentTarget] = useState(null);

  const filters = useMemo(
    () => ({
      studentProfileId: studentFilter,
      blockId: blockFilter
    }),
    [studentFilter, blockFilter]
  );

  useEffect(() => {
    const token = sessionStorage.getItem("accessToken");
    const role = sessionStorage.getItem("userRole");

    if (!token || role !== "ADMIN") {
      window.location.replace("/");
      return;
    }

    // The pickers are optional: the page still works without them.
    getStudentProfiles().then(setStudents).catch(() => {});
    getActiveBlocks().then(setBlocks).catch(() => {});
  }, []);

  useEffect(() => {
    const role = sessionStorage.getItem("userRole");

    if (role === "ADMIN") {
      loadReport();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filters]);

  async function loadReport() {
    setIsLoading(true);

    try {
      setRows(await getFeeReport(filters));
      setErrorMessage("");
    } catch (error) {
      setErrorMessage(
        errorText(error, "Unable to load the fee report.")
      );
    } finally {
      setIsLoading(false);
    }
  }

  async function handleDownload() {
    setIsDownloading(true);
    setErrorMessage("");

    try {
      await downloadFeeReportCsv(filters);
    } catch (error) {
      setErrorMessage(
        errorText(error, "Unable to download the report.")
      );
    } finally {
      setIsDownloading(false);
    }
  }

  function handleInvoiceSaved(invoice) {
    setShowInvoiceDialog(false);
    setSuccessMessage(
      `Invoice ${invoice.invoiceNumber} was created.`
    );
    loadReport();
  }

  function handlePaymentSaved(result) {
    setPaymentTarget(null);
    setSuccessMessage(
      `Payment ${result.paymentReference} recorded. Outstanding: ${formatAmount(
        result.outstandingAmount
      )}.`
    );
    loadReport();
  }

  const totals = useMemo(
    () =>
      rows.reduce(
        (sum, row) => ({
          total: sum.total + Number(row.totalAmount),
          paid: sum.paid + Number(row.paidAmount),
          outstanding:
            sum.outstanding + Number(row.outstandingAmount)
        }),
        { total: 0, paid: 0, outstanding: 0 }
      ),
    [rows]
  );

  return (
    <AppShell
      activePage="fees"
      eyebrow="FEES"
      title="Fee management"
      description="Issue invoices, record payments and download the fee status report."
      actions={
        <>
          <button
            type="button"
            className="secondary-button"
            disabled={isDownloading}
            onClick={handleDownload}
          >
            {isDownloading ? "Downloading..." : "Download CSV"}
          </button>

          <button
            type="button"
            className="primary-button"
            onClick={() => {
              setSuccessMessage("");
              setShowInvoiceDialog(true);
            }}
          >
            Create invoice
          </button>
        </>
      }
    >
      <div className="admin-users-page">
        <section className="admin-users-content">
          {successMessage && (
            <div className="message success" role="status">
              {successMessage}
            </div>
          )}

          {errorMessage && (
            <div className="message error" role="alert">
              {errorMessage}
            </div>
          )}

          <section className="admin-users-card">
            <div className="admin-users-toolbar">
              <div>
                <h2>Fee status report</h2>

                <span>
                  {rows.length} invoice
                  {rows.length === 1 ? "" : "s"} · total{" "}
                  {formatAmount(totals.total)} · paid{" "}
                  {formatAmount(totals.paid)} · outstanding{" "}
                  {formatAmount(totals.outstanding)}
                </span>
              </div>

              <div className="tools-filters">
                <div className="form-group">
                  <label htmlFor="feeStudentFilter">Student</label>

                  <select
                    id="feeStudentFilter"
                    value={studentFilter}
                    onChange={(event) =>
                      setStudentFilter(event.target.value)
                    }
                  >
                    <option value="">All students</option>
                    {students.map((student) => (
                      <option
                        key={student.studentProfileId}
                        value={student.studentProfileId}
                      >
                        {student.registrationNumber} ·{" "}
                        {student.username}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="form-group">
                  <label htmlFor="feeBlockFilter">Block</label>

                  <select
                    id="feeBlockFilter"
                    value={blockFilter}
                    onChange={(event) =>
                      setBlockFilter(event.target.value)
                    }
                  >
                    <option value="">All blocks</option>
                    {blocks.map((block) => (
                      <option
                        key={block.blockId}
                        value={block.blockId}
                      >
                        {block.blockCode} — {block.blockName}
                      </option>
                    ))}
                  </select>
                </div>
              </div>
            </div>

            {isLoading ? (
              <div className="admin-table-state">
                <div className="loading-spinner" />
                <p>Loading fee report...</p>
              </div>
            ) : rows.length === 0 ? (
              <div className="admin-table-state">
                <h3>No invoices found</h3>
                <p>
                  Create an invoice or change the filters.
                </p>
              </div>
            ) : (
              <div className="admin-table-wrapper">
                <table className="admin-users-table">
                  <thead>
                    <tr>
                      <th>Invoice</th>
                      <th>Student</th>
                      <th>Block</th>
                      <th>Fee type</th>
                      <th className="amount-cell">Total</th>
                      <th className="amount-cell">Paid</th>
                      <th className="amount-cell">Outstanding</th>
                      <th>Due</th>
                      <th>Status</th>
                      <th>
                        <span className="sr-only">Actions</span>
                      </th>
                    </tr>
                  </thead>

                  <tbody>
                    {rows.map((row) => (
                      <tr key={row.invoiceId}>
                        <td>{row.invoiceNumber}</td>

                        <td>
                          <strong>{row.studentName}</strong>
                          <br />
                          <small>{row.registrationNumber}</small>
                        </td>

                        <td>{row.blockCode ?? "—"}</td>
                        <td>{row.feeType}</td>

                        <td className="amount-cell">
                          {formatAmount(row.totalAmount)}
                        </td>
                        <td className="amount-cell">
                          {formatAmount(row.paidAmount)}
                        </td>
                        <td className="amount-cell">
                          {formatAmount(row.outstandingAmount)}
                        </td>

                        <td>{formatDate(row.dueDate)}</td>

                        <td>
                          <span
                            className={`status-badge ${row.status.toLowerCase()}`}
                          >
                            {row.status}
                          </span>
                        </td>

                        <td>
                          <div className="admin-row-actions">
                            <button
                              type="button"
                              disabled={
                                Number(row.outstandingAmount) <= 0
                              }
                              onClick={() => {
                                setSuccessMessage("");
                                setPaymentTarget(row);
                              }}
                            >
                              Record payment
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </section>

        {showInvoiceDialog && (
          <InvoiceDialog
            students={students}
            onCancel={() => setShowInvoiceDialog(false)}
            onSaved={handleInvoiceSaved}
          />
        )}

        {paymentTarget && (
          <PaymentDialog
            invoice={paymentTarget}
            onCancel={() => setPaymentTarget(null)}
            onSaved={handlePaymentSaved}
          />
        )}
      </div>
    </AppShell>
  );
}

export default AdminFeesPage;
