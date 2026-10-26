// Report viewer: builds the filter form from the report's parameters,
// renders the report through /Reports/Render (same origin, server-side SSRS)
// and shows the PDF inline. No SSRS URL or credential is used in the browser.
(function () {
    "use strict";

    var root = document.getElementById("report-viewer");
    if (!root) return;

    var reportKey = root.dataset.reportKey;
    var parametersUrl = root.dataset.parametersUrl;
    var renderUrl = root.dataset.renderUrl;

    var el = {
        form: document.getElementById("rv-form"),
        fields: document.getElementById("rv-fields"),
        notes: document.getElementById("rv-notes"),
        formError: document.getElementById("rv-form-error"),
        generate: document.getElementById("rv-generate"),
        reset: document.getElementById("rv-reset"),
        quickRanges: root.querySelector(".rv-quick-ranges"),
        status: document.getElementById("rv-status"),
        placeholder: document.getElementById("rv-placeholder"),
        error: document.getElementById("rv-error"),
        errorText: document.getElementById("rv-error-text"),
        noInline: document.getElementById("rv-no-inline"),
        frame: document.getElementById("rv-frame"),
        loading: document.getElementById("rv-loading"),
        loadingText: document.getElementById("rv-loading-text"),
        cancel: document.getElementById("rv-cancel"),
        print: document.getElementById("rv-print"),
        open: document.getElementById("rv-open"),
        downloads: root.querySelectorAll("[data-download]"),
        reportSelect: document.getElementById("rv-report-select")
    };

    var definition = null;     // filter definition from the server
    var currentBlobUrl = null; // PDF currently displayed
    var lastQuery = null;      // filter values used for the displayed report
    var inflight = null;       // AbortController of the running request
    var timer = null;
    var loadingSeq = 0;        // ignores completions of superseded requests

    // Browsers that cannot display a PDF inside an iframe (most phones).
    var canInlinePdf = navigator.pdfViewerEnabled !== false;

    // ------------------------------------------------------------------
    // Report selector
    // ------------------------------------------------------------------
    if (el.reportSelect) {
        el.reportSelect.addEventListener("change", function () {
            var url = el.reportSelect.dataset.showUrl + "?key=" + encodeURIComponent(el.reportSelect.value);
            window.location.href = url;
        });
    }

    // ------------------------------------------------------------------
    // Load filter definition
    // ------------------------------------------------------------------
    fetchJson(parametersUrl + "?key=" + encodeURIComponent(reportKey))
        .then(function (def) {
            definition = def;
            buildForm(def);
            applyValues(valuesFromUrl() || defaultValues());
            el.generate.disabled = false;
            el.reset.disabled = false;

            // A shared/bookmarked link with filters opens the report directly.
            if (valuesFromUrl()) generate();
        })
        .catch(function (err) {
            el.fields.innerHTML = "";
            showFormError("Filters could not be loaded. " + err.message);
            setStatus("Report unavailable");
        });

    // ------------------------------------------------------------------
    // Form building
    // ------------------------------------------------------------------
    function buildForm(def) {
        el.fields.innerHTML = "";
        el.notes.innerHTML = "";

        if (!def.fields || def.fields.length === 0) {
            var none = document.createElement("p");
            none.className = "rv-no-filters";
            none.textContent = "This report has no filters.";
            el.fields.appendChild(none);
        }

        (def.fields || []).forEach(function (f) {
            el.fields.appendChild(buildField(f));
        });

        (def.notes || []).forEach(function (n) {
            var li = document.createElement("li");
            li.textContent = n;
            el.notes.appendChild(li);
        });

        var hasRange = (def.fields || []).some(function (f) { return f.role === "from"; }) &&
                       (def.fields || []).some(function (f) { return f.role === "to"; });
        el.quickRanges.hidden = !hasRange;
    }

    function buildField(f) {
        var group = document.createElement("div");
        group.className = "filter-group rv-field" + (f.control === "checkbox" ? " rv-field-check" : "");
        group.dataset.name = f.name;

        var id = "rv-f-" + f.name;
        var label = document.createElement("label");
        label.setAttribute("for", id);
        label.textContent = f.label + (f.required ? " *" : "");

        var input;

        switch (f.control) {
            case "select":
            case "multiselect":
                input = document.createElement("select");
                if (f.control === "multiselect") input.multiple = true;
                if (f.emptyOptionLabel !== null && f.emptyOptionLabel !== undefined) {
                    input.appendChild(option("", f.emptyOptionLabel));
                } else if (f.control === "select") {
                    var placeholder = option("", "Select…");
                    placeholder.disabled = true;
                    input.appendChild(placeholder);
                }
                (f.options || []).forEach(function (o) { input.appendChild(option(o.value, o.label)); });
                break;

            case "checkbox":
                input = document.createElement("input");
                input.type = "checkbox";
                break;

            case "date":
                input = document.createElement("input");
                input.type = "date";
                break;

            case "datetime":
                input = document.createElement("input");
                input.type = "datetime-local";
                break;

            case "number":
                input = document.createElement("input");
                input.type = "number";
                input.inputMode = "numeric";
                if (f.min !== null && f.min !== undefined) input.min = f.min;
                if (f.max !== null && f.max !== undefined) input.max = f.max;
                input.step = f.step ? f.step : "any";
                break;

            default:
                input = document.createElement("input");
                input.type = "text";
                if (f.maxLength) input.maxLength = f.maxLength;
                if (f.placeholder) input.placeholder = f.placeholder;
        }

        input.id = id;
        input.name = f.name;
        if (f.required) input.required = true;
        input.addEventListener("input", function () { clearFieldError(f.name); });
        input.addEventListener("change", function () { clearFieldError(f.name); });

        if (f.control === "checkbox") {
            group.appendChild(input);
            group.appendChild(label);
        } else {
            group.appendChild(label);
            group.appendChild(input);
        }

        if (f.help) {
            var help = document.createElement("small");
            help.className = "rv-help";
            help.textContent = f.help;
            group.appendChild(help);
        }

        var err = document.createElement("small");
        err.className = "rv-field-error";
        err.hidden = true;
        group.appendChild(err);

        return group;
    }

    function option(value, text) {
        var o = document.createElement("option");
        o.value = value;
        o.textContent = text;
        return o;
    }

    function inputFor(name) {
        return el.form.querySelector('[name="' + cssEscape(name) + '"]');
    }

    function fieldDef(name) {
        return (definition.fields || []).filter(function (f) { return f.name === name; })[0];
    }

    // ------------------------------------------------------------------
    // Values
    // ------------------------------------------------------------------
    function defaultValues() {
        var values = {};
        (definition.fields || []).forEach(function (f) {
            if (f.control === "multiselect") values[f.name] = f.defaultValues || [];
            else values[f.name] = f.defaultValue === null || f.defaultValue === undefined ? "" : String(f.defaultValue);
        });
        return values;
    }

    function valuesFromUrl() {
        var params = new URLSearchParams(window.location.search);
        var values = {};
        var found = false;
        (definition.fields || []).forEach(function (f) {
            if (!params.has(f.name)) return;
            found = true;
            values[f.name] = f.control === "multiselect" ? params.getAll(f.name) : params.get(f.name);
        });
        if (!found) return null;

        // Fields missing from the link get their defaults.
        var defaults = defaultValues();
        Object.keys(defaults).forEach(function (k) { if (!(k in values)) values[k] = defaults[k]; });
        return values;
    }

    function applyValues(values) {
        (definition.fields || []).forEach(function (f) {
            var input = inputFor(f.name);
            if (!input) return;
            var v = values[f.name];

            if (f.control === "checkbox") {
                input.checked = String(v).toLowerCase() === "true";
            } else if (f.control === "multiselect") {
                var list = Array.isArray(v) ? v : (v ? [v] : []);
                Array.prototype.forEach.call(input.options, function (o) { o.selected = list.indexOf(o.value) >= 0; });
            } else if (f.control === "select") {
                var exists = Array.prototype.some.call(input.options, function (o) { return o.value === (v || ""); });
                input.value = exists ? (v || "") : "";
            } else {
                input.value = v === null || v === undefined ? "" : v;
            }
        });
        clearErrors();
    }

    function collectValues() {
        var values = [];
        (definition.fields || []).forEach(function (f) {
            var input = inputFor(f.name);
            if (!input) return;

            if (f.control === "checkbox") {
                values.push([f.name, input.checked ? "true" : "false"]);
            } else if (f.control === "multiselect") {
                Array.prototype.forEach.call(input.selectedOptions, function (o) { values.push([f.name, o.value]); });
            } else {
                values.push([f.name, (input.value || "").trim()]);
            }
        });
        return values;
    }

    // ------------------------------------------------------------------
    // Validation (the server validates again)
    // ------------------------------------------------------------------
    function validate() {
        clearErrors();
        var ok = true;
        var from = null, to = null, toName = null;

        (definition.fields || []).forEach(function (f) {
            var input = inputFor(f.name);
            if (!input) return;

            var value = f.control === "multiselect"
                ? input.selectedOptions.length ? "x" : ""
                : (input.value || "").trim();

            if (f.required && f.control !== "checkbox" && !value) {
                setFieldError(f.name, f.label + " is required.");
                ok = false;
                return;
            }

            if (value && input.validity && !input.validity.valid) {
                var msg = f.label + " is not valid.";
                if (f.control === "number" && (f.min !== null || f.max !== null)) {
                    msg = f.label + " must be between " + f.min + " and " + f.max + ".";
                }
                setFieldError(f.name, msg);
                ok = false;
                return;
            }

            if (f.role === "from" && value) from = value;
            if (f.role === "to" && value) { to = value; toName = f.name; }
        });

        if (from && to && from > to) {
            setFieldError(toName, "The To date must be on or after the From date.");
            ok = false;
        }

        return ok;
    }

    function setFieldError(name, message) {
        var group = el.fields.querySelector('.rv-field[data-name="' + cssEscape(name) + '"]');
        if (!group) {
            showFormError(message);
            return;
        }
        group.classList.add("has-error");
        var err = group.querySelector(".rv-field-error");
        err.textContent = message;
        err.hidden = false;
    }

    function clearFieldError(name) {
        var group = el.fields.querySelector('.rv-field[data-name="' + cssEscape(name) + '"]');
        if (!group) return;
        group.classList.remove("has-error");
        var err = group.querySelector(".rv-field-error");
        err.textContent = "";
        err.hidden = true;
    }

    function clearErrors() {
        Array.prototype.forEach.call(el.fields.querySelectorAll(".rv-field"), function (g) {
            clearFieldError(g.dataset.name);
        });
        el.formError.hidden = true;
        el.formError.textContent = "";
    }

    function showFormError(message) {
        el.formError.textContent = message;
        el.formError.hidden = false;
    }

    // ------------------------------------------------------------------
    // Generate / view
    // ------------------------------------------------------------------
    el.form.addEventListener("submit", function (e) {
        e.preventDefault();
        generate();
    });

    el.reset.addEventListener("click", function () {
        if (!definition) return;
        applyValues(defaultValues());
    });

    el.quickRanges.addEventListener("click", function (e) {
        var btn = e.target.closest("[data-range]");
        if (!btn || !definition) return;
        var days = parseInt(btn.dataset.range, 10);
        var today = new Date();
        var utcToday = new Date(Date.UTC(today.getUTCFullYear(), today.getUTCMonth(), today.getUTCDate()));
        var start = new Date(utcToday.getTime() - days * 86400000);

        (definition.fields || []).forEach(function (f) {
            var input = inputFor(f.name);
            if (!input) return;
            if (f.role === "from") input.value = isoDate(start);
            if (f.role === "to") input.value = isoDate(utcToday);
            clearFieldError(f.name);
        });
    });

    el.cancel.addEventListener("click", function () {
        if (inflight) inflight.abort();
    });

    function generate() {
        if (!definition || !validate()) return;

        var values = collectValues();
        var query = buildQuery(values, "PDF", false);

        var seq = startLoading("Generating report…");

        request(query)
            .then(function (result) {
                lastQuery = values;
                showPdf(result.blob);
                setStatus("Generated " + new Date().toLocaleString() + " · " + formatSize(result.blob.size));
                updateUrl(values);
            })
            .catch(handleError)
            .then(function () { stopLoading(seq); });
    }

    function showPdf(blob) {
        if (currentBlobUrl) URL.revokeObjectURL(currentBlobUrl);
        currentBlobUrl = URL.createObjectURL(blob);

        el.placeholder.hidden = true;
        el.error.hidden = true;

        if (canInlinePdf) {
            el.noInline.hidden = true;
            el.frame.hidden = false;
            el.frame.src = currentBlobUrl + "#view=FitH";
        } else {
            el.frame.hidden = true;
            el.noInline.hidden = false;
        }

        setActions(true);
    }

    function handleError(err) {
        if (err && err.name === "AbortError") {
            setStatus("Generation cancelled");
            return;
        }

        if (err && err.fieldErrors) {
            var shown = false;
            Object.keys(err.fieldErrors).forEach(function (name) {
                (err.fieldErrors[name] || []).forEach(function (m) {
                    if (name) { setFieldError(name, m); shown = true; }
                    else showFormError(m);
                });
            });
            if (shown && !err.fieldErrors[""]) showFormError(err.message);
            setStatus("Please correct the filters");
            return;
        }

        el.placeholder.hidden = true;
        el.frame.hidden = true;
        el.noInline.hidden = true;
        el.errorText.textContent = err && err.message ? err.message : "Unexpected error.";
        el.error.hidden = false;
        setActions(false);
        setStatus("Report failed");
    }

    // ------------------------------------------------------------------
    // Toolbar actions
    // ------------------------------------------------------------------
    el.print.addEventListener("click", function () {
        if (!currentBlobUrl) return;
        try {
            el.frame.contentWindow.focus();
            el.frame.contentWindow.print();
        } catch (e) {
            window.open(currentBlobUrl, "_blank", "noopener");
        }
    });

    el.open.addEventListener("click", function () {
        if (currentBlobUrl) window.open(currentBlobUrl, "_blank", "noopener");
    });

    Array.prototype.forEach.call(el.downloads, function (btn) {
        btn.addEventListener("click", function () {
            if (!lastQuery) return;
            var format = btn.dataset.download;
            var query = buildQuery(lastQuery, format, true);

            var seq = startLoading("Preparing " + btn.textContent.trim() + " download…");

            request(query)
                .then(function (result) {
                    saveBlob(result.blob, result.fileName || (reportKey + "." + format.toLowerCase()));
                })
                .catch(function (err) {
                    if (err && err.name === "AbortError") return;
                    showFormError("Download failed: " + err.message);
                })
                .then(function () { stopLoading(seq); });
        });
    });

    function setActions(enabled) {
        el.print.disabled = !enabled || !canInlinePdf;
        el.open.disabled = !enabled;
        Array.prototype.forEach.call(el.downloads, function (b) { b.disabled = !enabled; });
    }

    // ------------------------------------------------------------------
    // HTTP
    // ------------------------------------------------------------------
    function buildQuery(values, format, download) {
        var q = new URLSearchParams();
        q.append("key", reportKey);
        q.append("format", format);
        if (download) q.append("download", "true");
        values.forEach(function (pair) { q.append(pair[0], pair[1]); });
        return q;
    }

    function request(query) {
        if (inflight) inflight.abort();
        inflight = new AbortController();

        return fetch(renderUrl + "?" + query.toString(), {
            method: "GET",
            credentials: "same-origin",
            signal: inflight.signal,
            headers: { "Accept": "application/pdf, application/octet-stream, application/json" }
        }).then(function (response) {
            var type = response.headers.get("Content-Type") || "";

            if (!response.ok || type.indexOf("application/json") === 0) {
                return response.json().catch(function () { return {}; }).then(function (body) {
                    var e = new Error(body.message || httpMessage(response.status));
                    if (body.errors) e.fieldErrors = body.errors;
                    throw e;
                });
            }

            return response.blob().then(function (blob) {
                return { blob: blob, fileName: fileNameFrom(response.headers.get("Content-Disposition")) };
            });
        }, function (err) {
            if (err && err.name === "AbortError") throw err;
            throw new Error("The server could not be reached. Check your network connection and try again.");
        });
    }

    function fetchJson(url) {
        return fetch(url, { credentials: "same-origin", headers: { "Accept": "application/json" } })
            .then(function (response) {
                return response.json().catch(function () { return {}; }).then(function (body) {
                    if (!response.ok) throw new Error(body.message || httpMessage(response.status));
                    return body;
                });
            }, function () {
                throw new Error("The server could not be reached.");
            });
    }

    function httpMessage(status) {
        if (status === 401 || status === 403) return "You are not allowed to run this report.";
        if (status === 404) return "The report was not found.";
        if (status === 504) return "The report took too long to generate. Try a shorter date range.";
        return "The server returned an error (" + status + ").";
    }

    // ------------------------------------------------------------------
    // UI helpers
    // ------------------------------------------------------------------
    function startLoading(text) {
        el.loadingText.textContent = text;
        el.loading.hidden = false;
        el.generate.disabled = true;
        var started = Date.now();
        clearInterval(timer);
        timer = setInterval(function () {
            var s = Math.round((Date.now() - started) / 1000);
            if (s >= 3) el.loadingText.textContent = text + " (" + s + " s)";
        }, 1000);
        return ++loadingSeq;
    }

    function stopLoading(seq) {
        if (seq !== loadingSeq) return; // a newer request is still running
        clearInterval(timer);
        el.loading.hidden = true;
        el.generate.disabled = false;
        inflight = null;
    }

    function setStatus(text) {
        el.status.textContent = text;
    }

    function updateUrl(values) {
        if (!window.history || !window.history.replaceState) return;
        var q = new URLSearchParams();
        q.append("key", reportKey);
        values.forEach(function (pair) { q.append(pair[0], pair[1]); });
        window.history.replaceState(null, "", window.location.pathname + "?" + q.toString());
    }

    function saveBlob(blob, fileName) {
        var url = URL.createObjectURL(blob);
        var a = document.createElement("a");
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        a.remove();
        setTimeout(function () { URL.revokeObjectURL(url); }, 10000);
    }

    function fileNameFrom(disposition) {
        if (!disposition) return null;
        var star = /filename\*=UTF-8''([^;]+)/i.exec(disposition);
        if (star) {
            try { return decodeURIComponent(star[1]); } catch (e) { /* fall through */ }
        }
        var plain = /filename="?([^";]+)"?/i.exec(disposition);
        return plain ? plain[1] : null;
    }

    function formatSize(bytes) {
        if (bytes < 1024) return bytes + " B";
        if (bytes < 1024 * 1024) return Math.round(bytes / 1024) + " KB";
        return (bytes / 1024 / 1024).toFixed(1) + " MB";
    }

    function isoDate(d) {
        return d.getUTCFullYear() + "-" + pad(d.getUTCMonth() + 1) + "-" + pad(d.getUTCDate());
    }

    function pad(n) { return n < 10 ? "0" + n : String(n); }

    function cssEscape(value) {
        return window.CSS && CSS.escape ? CSS.escape(value) : String(value).replace(/["\\]/g, "\\$&");
    }

    window.addEventListener("beforeunload", function () {
        if (currentBlobUrl) URL.revokeObjectURL(currentBlobUrl);
    });
})();
