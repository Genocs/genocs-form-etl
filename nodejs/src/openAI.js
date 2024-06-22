const openai = require('openai');

const openaiClient = new openai.OpenAI();

const is_taxfree_form_prompt = [
    {
        role: "assistant",
        content: [
            {
                type: "text",
                text: "You are an assistant to help identify if the image provided is a Tax-Free form. In Italy private companies issue the forms while in Thailand they are issued by a national authority. Companies valid in Italy are: 'Global Blue', 'Planet', 'Tax Refund'. Only Tax Free forms are accepted. The Tax-free form could be a standard A4 sheet or a thermal receipt. Please respond concisely, starting by reporting the country of origin, the issuing company and the type of printer. Please replay only with a JSON like the following: { is_taxfree_form : true, is_thermal_printed: true, country: 'USA', vro: 'tax operator' }"
            }
        ]
    },
    {
        role: "user",
        content: [
            {
                type: "text",
                text: "Is the image a TaxFree form?"
            },
            {
                type: "image_url",
                image_url: ""
            }
        ]
    }
];

const get_taxfree_form_info = [
    {
        role: "assistant",
        content: [
            {
                type: "text",
                text: "You are an assistant to help identify if the image provided is a Tax-Free form. In Italy private companies issue the forms while in Thailand they are issued by a national authority. Companies valid in Italy are: 'Global Blue', 'Planet', 'Tax Refund'. Only Tax Free forms are accepted. The Tax-free form could be a standard A4 sheet or a thermal receipt. Please respond friendly, in case the image is not a TaxFree form."
            }
        ]
    },
    {
        role: "user",
        content: [
            {
                type: "text",
                text: "Is the image a TaxFree form?"
            },
            {
                type: "image_url",
                image_url: ""
            }
        ]
    }
];


// Call the OpenAI API to check if the image is a Tax-Free form
async function isTaxFreeForm(url) {
    
    // Set the image URL in the prompt
    is_taxfree_form_prompt[1].content[1].image_url = url;
    return openaiClient.chat.completions.create({
        model: "gpt-4-vision-preview",
        messages: is_taxfree_form_prompt,
        max_tokens: 512,
        temperature: 0,
    });
}


// Call the OpenAI API to get the information about the Tax-Free form
async function getTaxFreeFormInfo(url) {

    // Set the image URL in the prompt
    get_taxfree_form_info[1].content[1].image_url = url;
    return openaiClient.chat.completions.create({
        model: "gpt-4-vision-preview",
        messages: get_taxfree_form_info,
        max_tokens: 512,
        temperature: 0,
    });
}



// Exporting functions to make them accessible from other files
module.exports = {
    isTaxFreeForm,
    getTaxFreeFormInfo,
};

// exports.isTaxFreeForm = isTaxFreeForm;
// exports.getTaxFreeFormInfo = getTaxFreeFormInfo;
// exports.getFileAccessToken = getFileAccessToken;
